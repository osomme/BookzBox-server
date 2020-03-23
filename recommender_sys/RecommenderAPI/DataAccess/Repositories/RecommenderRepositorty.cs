using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models;
using Neo4j.Driver;

namespace BooxBox.DataAccess.Repositories
{
    public class RecommenderRepository : BaseRepository, IRecommenderRepository
    {
        private readonly IBoxRecordMapper _boxMapper;

        public RecommenderRepository(IDatabase db, IBoxRecordMapper boxMapper)
             : base(db)
        {
            _boxMapper = boxMapper ?? throw new ArgumentNullException(nameof(boxMapper));
        }

        /// <summary>
        /// Fetch a set amount of box recommendations for a given user.
        /// </summary>
        /// <param name="userId">Id of the user of whom to fetch boxes for.</param>
        /// <param name="limit">The maximum amount of results to return.</param>
        /// <returns>List of boxes - can be empty</returns>
        public async Task<IEnumerable<Box>> FetchRecommendationsAsync(string userId, uint limit)
        {
            List<Tuple<uint, Box>> weight_box = new List<Tuple<uint, Box>>();

            weight_box.AddRange(GetBoxesAsTuples(await FetchBoxesWithSubjectsMatchingThoseOfMyBoxes(userId, limit), 2));
            weight_box.AddRange(GetBoxesAsTuples(await FetchBoxesWithSubjectsMatchingPreferences(userId, limit), 7));
            weight_box.AddRange(GetBoxesAsTuples(await FetchBoxesWithSubjectsMatchingLikedBoxes(userId, limit), 4));
            weight_box.AddRange(GetBoxesAsTuples(await FetchBoxesFromMyLikes(userId, limit), 1));
            weight_box.AddRange(GetBoxesAsTuples(await FetchBoxesFromOthersLikes(userId, limit), 1));
            // TODO: Add weight based on box location.
            // TODO: Mark boxes as recommended so they don't get recommended multiple times.
            return GetHighestWeightedBoxes(weight_box).Take(limit.As<int>(0));
        }

        private List<Tuple<uint, Box>> GetBoxesAsTuples(IEnumerable<Box> boxes, uint weight)
        {
            List<Tuple<uint, Box>> result = new List<Tuple<uint, Box>>();

            foreach (Box box in boxes)
            {
                result.Add(new Tuple<uint, Box>(weight, box));
            }

            return result;
        }

        /// <summary>
        /// Get boxes by weight.
        /// </summary>
        /// <param name="weight_box">List of boxes and weights as tuples.</param>
        /// <returns>Returns the boxes sorted by weight. The boxes with heighest weight first.</returns>
        private List<Box> GetHighestWeightedBoxes(List<Tuple<uint, Box>> weight_boxes)
        {
            List<Box> result = new List<Box>();
            // Contains the total weight for each box.
            Dictionary<Box, uint> weight_map = new Dictionary<Box, uint>();

            foreach (var weight_box in weight_boxes)
            {
                uint w;
                bool boxExists = weight_map.TryGetValue(weight_box.Item2, out w);
                if (boxExists)
                {
                    weight_map[weight_box.Item2] = w + weight_box.Item1;
                }
                else
                {
                    uint totalW = weight_box.Item1 + GetPublishDateWeight(weight_box.Item2);
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
        private uint GetPublishDateWeight(Box box)
        {
            var published = DateTimeOffset.FromUnixTimeMilliseconds(box.publishDateTime).Date;
            var now = DateTimeOffset.Now.Date;
            var dayDiff = (now - published).TotalDays;
            if (dayDiff > 30)
            {
                return 0;
            }
            else if (dayDiff > 14)
            {
                return 1;
            }
            else if (dayDiff > 7)
            {
                return 2;
            }
            else
            {
                return 4;
            }
        }

        private async Task<IEnumerable<Box>> FetchBoxesAsync(string userId, uint limit, string query)
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
        private async Task<IEnumerable<Box>> FetchBoxesWithSubjectsMatchingThoseOfMyBoxes(string userId, uint limit)
        {
            return await FetchBoxesAsync(
                userId,
                limit,
                $"MATCH (user:User {{userId: '{userId}'}})-[:PUBlISHED]-(:Box)-[:PART_OF]-(:Book)-[:HAS_SUBJECT]-(s:Subject)-[:IN_BOOK]->(book:Book)-[*0..1]-(box:Box) " +
                "WHERE box.status = 0 and box.publisherId <> user.userId " +
                "RETURN box, collect(book) as books, collect(s) as subjects " +
                $"LIMIT {limit}"
            );
        }



        /// <summary>
        /// Fetches all boxes that have subjects matching the preffered subjects
        /// for the given user.
        /// </summary>
        /// <param name="userId">Id of the user of whom to fetch boxes for.</param>
        /// <param name="limit">The maximum amount of results to return.</param>
        /// <returns>List of public boxes if successful, otherwise null.</returns>
        private async Task<IEnumerable<Box>> FetchBoxesWithSubjectsMatchingPreferences(string userId, uint limit)
        {
            return await FetchBoxesAsync(
                userId,
                limit,
                $"MATCH (user:User {{userId: '{userId}'}})-[:PREFERS]-(s:Subject)-[:IN_BOOK]-(book:Book)-[:PART_OF]-(box:Box) " +
                "WHERE box.status = 0 and box.publisherId <> user.userId " +
                "RETURN box, collect(book) as books " +
                $"LIMIT {limit}"
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
        private async Task<IEnumerable<Box>> FetchBoxesWithSubjectsMatchingLikedBoxes(string userId, uint limit)
        {
            return await FetchBoxesAsync(
                userId,
                limit,
                $"MATCH (user:User {{userId: '{userId}'}})-[:LIKES]-(:Box)-[:PART_OF]-(:Book)-[:HAS_SUBJECT]-(s:Subject)-[:IN_BOOK]->(book:Book)-[*0..1]-(box:Box) " +
                "WHERE box.status = 0 and box.publisherId <> user.userId " +
                "RETURN box, collect(book) as books " +
                $"LIMIT {limit}"
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
        private async Task<IEnumerable<Box>> FetchBoxesFromMyLikes(string userId, uint limit)
        {
            return await FetchBoxesAsync(
                userId,
                limit,
                $"MATCH (user:User {{userId: '{userId}'}})-[:LIKES]-(:Box)-[:PUBlISHED]-(publisher:User)-[:PUBlISHED]-(box:Box)-[:PART_OF]-(book:Book) " +
                "WHERE box.status = 0 AND box.publisherId <> user.userId " +
                "RETURN box, collect(book) as books " +
                $"LIMIT {limit}"
            );
        }

        /// <summary>
        /// Fetches the boxes that are published by users that liked your boxes.
        /// This gets boxes from people that may have the same interests as the passed user.
        /// </summary>
        /// <param name="userId">Id of the user of whom to fetch boxes for.</param>
        /// <param name="limit">The maximum amount of results to return.</param>
        /// <returns>List of public boxes if successful, otherwise null.</returns>
        private async Task<IEnumerable<Box>> FetchBoxesFromOthersLikes(string userId, uint limit)
        {
            return await FetchBoxesAsync(
                userId,
                limit,
                $"MATCH (book:Book)-[:PART_OF]-(box:Box)-[:PUBlISHED]-(publisher:User)-[:LIKES]-(myBox:Box) " +
                $"WHERE myBox.publisherId = '{userId}' AND box.status = 0 " +
                "RETURN box, collect(book) AS books " +
                $"LIMIT {limit}"
            );
        }
    }
}
