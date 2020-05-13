using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusNY;
using Models;
using Neo4j.Driver;

namespace BooxBox.DataAccess.Repositories
{
    public class RecommenderRepository : BaseRepository, IRecommenderRepository
    {
        private const int VERY_HIGH_IMPORTANCE = 8;
        private const int HIGH_IMPORTANCE = 7;
        private const int MEDIUM_IMPORTANCE = 4;
        private const int LOW_IMPORTANCE = 2;
        private const int VERY_LOW_IMPORTANCE = 1;
        private const int NO_IMPORTANCE = -MEDIUM_IMPORTANCE;
        private const int IRRELEVANT_IMPORTANCE = -VERY_HIGH_IMPORTANCE;

        private const double LARGE_AREA = 4.0;
        private const double SMALL_AREA = 1.0;

        private readonly IBoxRecordMapper _boxMapper;

        public RecommenderRepository(IDatabase db, IBoxRecordMapper boxMapper)
             : base(db)
        {
            _boxMapper = boxMapper ?? throw new ArgumentNullException(nameof(boxMapper));
        }

        /// <summary>
        /// Fetch a set amount of box recommendations for a given user at the 
        /// location represented by the given coordiantes.
        /// </summary>
        /// <param name="userId">Id of the user of whom to fetch boxes for.</param>
        /// <param name="limit">The maximum amount of results to return.</param>
        /// <param name="latitude">The latitude coordinate of the location to get recommendations for.</param>
        /// <param name="longitude">The longitude coordinate of the location to get recommendations for.</param>
        /// <param name="mark">
        /// If true, marks the recommended boxes such that they will
        /// not be recommended again.
        /// </param>
        /// <returns>List of boxes - can be empty</returns>
        public async Task<IEnumerable<Box>> FetchRecommendationsAsync(
            string userId,
            int limit,
            double latitude,
            double longitude)
        {
            List<Tuple<int, Box>> weight_box = new List<Tuple<int, Box>>();

            weight_box.AddRange(GetBoxesAsTuples(await FetchBoxesWithSubjectsMatchingThoseOfMyBoxes(userId, limit), LOW_IMPORTANCE));
            weight_box.AddRange(GetBoxesAsTuples(await FetchBoxesWithSubjectsMatchingPreferences(userId, limit), HIGH_IMPORTANCE));
            weight_box.AddRange(GetBoxesAsTuples(await FetchBoxesWithSubjectsMatchingLikedBoxes(userId, limit), MEDIUM_IMPORTANCE));
            weight_box.AddRange(GetBoxesAsTuples(await FetchBoxesFromMyLikes(userId, limit), VERY_LOW_IMPORTANCE));
            weight_box.AddRange(GetBoxesAsTuples(await FetchBoxesFromOthersLikes(userId, limit), VERY_LOW_IMPORTANCE));

            if (weight_box.Count < limit)
            {
                weight_box.AddRange(GetBoxesAsTuples(await FetchFallbackBoxes(userId, limit - weight_box.Count, latitude, longitude), NO_IMPORTANCE));
            }

            var recommendations = GetHighestWeightedBoxes(weight_box, latitude, longitude).Take(limit).ToList();

            await MarkAllBoxesAsync(userId, recommendations);

            return recommendations;
        }

        private List<Tuple<int, Box>> GetBoxesAsTuples(IEnumerable<Box> boxes, int weight)
        {
            List<Tuple<int, Box>> result = new List<Tuple<int, Box>>();

            foreach (Box box in boxes)
            {
                result.Add(new Tuple<int, Box>(weight, box));
            }

            return result;
        }

        /// <summary>
        /// Get boxes by weight.
        /// </summary>
        /// <param name="weight_box">List of boxes and weights as tuples.</param>
        /// <returns>Returns the boxes sorted by weight. The boxes with heighest weight first.</returns>
        private List<Box> GetHighestWeightedBoxes(List<Tuple<int, Box>> weight_boxes, double latitude, double longitude)
        {
            List<Box> result = new List<Box>();
            // Contains the total weight for each box.
            Dictionary<Box, int> weight_map = new Dictionary<Box, int>();

            bool shouldUseLocation = (latitude > 0 && longitude > 0);

            foreach (var weight_box in weight_boxes)
            {
                int w;
                bool boxExists = weight_map.TryGetValue(weight_box.Item2, out w);
                if (boxExists)
                {
                    weight_map[weight_box.Item2] = w + weight_box.Item1;
                }
                else
                {
                    int totalW = weight_box.Item1;

                    // Add weight based on time since publish.
                    totalW += GetPublishDateWeight(weight_box.Item2);

                    // Add weight based on users distance from box.
                    if (shouldUseLocation)
                    {
                        totalW += GetLocationWeight(weight_box.Item2, latitude, longitude);
                    }

                    weight_map.Add(weight_box.Item2, totalW);
                }
            }

            var sorted = weight_map.OrderByDescending(x => x.Value).ToList();
            sorted.ForEach(kv => result.Add(kv.Key));

            return result;
        }

        /// <summary>
        /// Calculates the amount of weight to give the box based on
        /// the date it was published. More recent boxes get higher weight.
        /// </summary>
        /// <param name="box">The box to get the weight for.</param>
        /// <returns>A weight</returns>
        private int GetPublishDateWeight(Box box)
        {
            var published = DateTimeOffset.FromUnixTimeMilliseconds(box.publishDateTime).Date;
            var now = DateTimeOffset.Now.Date;
            var dayDiff = (now - published).TotalDays;
            if (dayDiff > 30)
            {
                return NO_IMPORTANCE;
            }
            else if (dayDiff > 21)
            {
                return 0;
            }
            else if (dayDiff > 7)
            {
                return LOW_IMPORTANCE;
            }
            else
            {
                return HIGH_IMPORTANCE;
            }
        }

        private int GetLocationWeight(Box box, double latitude, double longitude)
        {
            double distanceKM = CalculateDistance(box.Latitude, box.Longitude, latitude, longitude);

            if (distanceKM > 120)
            {
                return NO_IMPORTANCE;
            }
            else if (distanceKM > 60)
            {
                return LOW_IMPORTANCE;
            }
            else if (distanceKM > 20)
            {
                return MEDIUM_IMPORTANCE;
            }
            else
            {
                return HIGH_IMPORTANCE;
            }
        }

        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            return GIS.DistanceHaversine(lat1, lng1, lat2, lng2, UnitSystem.SI);
        }

        private async Task<IEnumerable<Box>> FetchBoxesAsync(string query)
        {
            List<Box> result = null;
            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(query);

                result = await cursor.ToListAsync(record => _boxMapper.Map(record));

                await cursor.ConsumeAsync();

            }
            finally
            {
                await _database.CloseSessionAsync();
            }
            return result;
        }

        /// <summary>
        /// Fetches boxes that have the same subjects as the boxes that the user with the
        /// given user id has published.
        /// </summary>
        /// <param name="userId">Id of the user of whom to fetch boxes for.</param>
        /// <param name="limit">The maximum amount of results to return.</param>
        /// <returns>List of public boxes if successful, otherwise null.</returns>
        private async Task<IEnumerable<Box>> FetchBoxesWithSubjectsMatchingThoseOfMyBoxes(string userId, int limit)
        {
            return await FetchBoxesAsync(
                $"MATCH (user:User {{userId: '{userId}'}})-[:PUBlISHED]-(:Box)-[:PART_OF]-(:Book)-[:HAS_SUBJECT]-(s:Subject)-[:IN_BOOK]->(book:Book)-[:PART_OF]-(box:Box) " +
                "WHERE box.status = 0 AND box.publisherId <> user.userId AND NOT (user)-[:READ]->(box) " +
                $"RETURN box, collect(book) as books, collect(s) as subjects LIMIT {limit}"
            );
        }



        /// <summary>
        /// Fetches all boxes that have subjects matching the preffered subjects
        /// for the given user.
        /// </summary>
        /// <param name="userId">Id of the user of whom to fetch boxes for.</param>
        /// <param name="limit">The maximum amount of results to return.</param>
        /// <returns>List of public boxes if successful, otherwise null.</returns>
        private async Task<IEnumerable<Box>> FetchBoxesWithSubjectsMatchingPreferences(string userId, int limit)
        {
            return await FetchBoxesAsync(
                $"MATCH (user:User {{userId: '{userId}'}})-[:PREFERS]-(s:Subject)-[:IN_BOOK]-(book:Book)-[:PART_OF]-(box:Box) " +
                "WHERE box.status = 0 and box.publisherId <> user.userId AND NOT (user)-[:READ]->(box) " +
                $"RETURN box, collect(book) as books LIMIT {limit}"
            );
        }

        /// <summary>
        /// Fetches all boxes that have subjects that match the subjects of the boxes
        /// that the user has liked.
        /// E.g Liking a box with subject = [ 'Fiction', 'Romance' ] returns
        /// all boxes that have those same subjects.
        /// </summary>
        /// <param name="userId">Id of the user of whom to fetch boxes for.</param>
        /// <param name="limit">The maximum amount of results to return.</param>
        /// <returns>List of public boxes if successful, otherwise null.</returns>
        private async Task<IEnumerable<Box>> FetchBoxesWithSubjectsMatchingLikedBoxes(string userId, int limit)
        {
            return await FetchBoxesAsync(
                $"MATCH (user:User {{userId: '{userId}'}})-[:LIKES]-(:Box)-[:PART_OF]-(:Book)-[:HAS_SUBJECT]-(s:Subject)-[:IN_BOOK]->(book:Book)-[:PART_OF]-(box:Box) " +
                "WHERE box.status = 0 and box.publisherId <> user.userId AND NOT (user)-[:READ]->(box) " +
                $"RETURN box, collect(book) as books LIMIT {limit}"
            );
        }

        /// <summary>
        /// Fetches all boxes that are published by a user of which the passed user has liked.
        /// This is based on the assumption that a user may publish multiple boxes with the 
        /// same theme and can thus be relevant for the user.
        /// </summary>
        /// <param name="userId">Id of the user of whom to fetch boxes for.</param>
        /// <param name="limit">The maximum amount of results to return.</param>
        /// <returns>List of public boxes if successful, otherwise null.</returns>
        private async Task<IEnumerable<Box>> FetchBoxesFromMyLikes(string userId, int limit)
        {
            return await FetchBoxesAsync(
                $"MATCH (user:User {{userId: '{userId}'}})-[:LIKES]-(:Box)-[:PUBlISHED]-(publisher:User)-[:PUBlISHED]-(box:Box)-[:PART_OF]-(book:Book) " +
                "WHERE box.status = 0 AND box.publisherId <> user.userId AND NOT (user)-[:READ]->(box) " +
                $"RETURN box, collect(book) as books LIMIT {limit}"
            );
        }

        /// <summary>
        /// Fetches the boxes that are published by users that liked your boxes.
        /// This gets boxes from people that may have the same interests as the passed user.
        /// </summary>
        /// <param name="userId">Id of the user of whom to fetch boxes for.</param>
        /// <param name="limit">The maximum amount of results to return.</param>
        /// <returns>List of public boxes if successful, otherwise null.</returns>
        private async Task<IEnumerable<Box>> FetchBoxesFromOthersLikes(string userId, int limit)
        {
            return await FetchBoxesAsync(
                $"MATCH (book:Book)-[:PART_OF]-(box:Box)-[:PUBlISHED]-(publisher:User)-[:LIKES]-(myBox:Box) " +
                $"MATCH (user:User{{userId: '{userId}'}}) " +
                $"WHERE myBox.publisherId = '{userId}' AND box.status = 0 AND NOT (user)-[:READ]->(box) " +
                $"RETURN box, collect(book) AS books LIMIT {limit}"
            );
        }

        private async Task MarkAllBoxesAsync(string userId, IEnumerable<Box> boxes)
        {
            foreach (Box box in boxes)
            {
                await MarkBoxAsync(userId, box);
            }
        }

        private async Task MarkBoxAsync(string userId, Box box)
        {
            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(
                    $"MATCH (user:User {{userId: '{userId}'}}),(box:Box {{boxId: '{box.Id}'}}) " +
                    "MERGE (user)-[r:READ]->(box)"
                );
                await cursor.ConsumeAsync();
            }
            finally
            {
                await _database.CloseSessionAsync();
            }
        }

        /// <summary>
        /// Retreives limit amount of boxes that the user has not published. These boxes are 
        /// ordered by publish date fetching the most recent first.
        /// </summary>
        private async Task<IEnumerable<Box>> FetchFallbackBoxes(
            string userId,
            int limit,
            double latitude,
            double longitude)
        {
            HashSet<Box> boxes = new HashSet<Box>(await FetchBoxesInArea(userId, limit, new Area(latitude, longitude, SMALL_AREA), false));

            if (boxes.Count() < limit)
            {
                boxes = new HashSet<Box>(boxes.Concat(await FetchBoxesInArea(userId, limit, new Area(latitude, longitude, LARGE_AREA), true)));
            }
            return boxes;
        }

        /// <summary>
        /// Retreives limit amount of boxes within a specified area. Boxes published by the given
        /// user are ignored. These boxes are ordered by publish date fetching the most recent first.
        /// </summary>
        /// <param name="marked">Should previously marked boxes be included?</param>
        private async Task<IEnumerable<Box>> FetchBoxesInArea(string userId, int limit, Area area, bool marked)
        {
            string query =
                $"MATCH (book:Book)-[:PART_OF]-(box:Box) " +
                $"MATCH (user:User{{userId: '{userId}'}}) " +
                $"WHERE " +
                $"NOT box.publisherId = '{userId}' " +
                $"AND box.status = 0 " +
                $"AND box.lat > {area.lat - area.d} " +
                $"AND box.lat < {area.lat + area.d} " +
                $"AND box.lng > {area.lng - area.d} " +
                $"AND box.lng < {area.lng + area.d} ";

            if (!marked)
            {
                query += $"AND NOT (user)-[:READ]->(box) ";
            }

            query += $"RETURN box, collect(book) AS books ORDER BY box.publishedOn DESC LIMIT {limit} ";

            return await FetchBoxesAsync(query);
        }

    }
}
