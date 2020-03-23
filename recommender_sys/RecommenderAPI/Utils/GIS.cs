/*****************************************************************************************
Module           :  GIS.cs |Class Lib
Description      :  Calculate distance between two geo-points on surface
*****************************************************************************************
Author           :  Alexander Bell
Copyright        :  2011-2015 Infosoft International Inc
*****************************************************************************************
DISCLAIMER       :  This Module is provided on AS IS basis without any warranty
*****************************************************************************************
TERMS OF USE     :  This module is copyrighted. Please keep the Copyright notice intact.
*****************************************************************************************/
using System;

namespace BusNY
{
    public enum UnitSystem { SI = 0, US = 1 }

    public static class GIS
    {
        internal static double EarthRadiusKm { get { return _radiusEarthKM; } }
        internal static double EarthRadiusMiles { get { return _radiusEarthMiles; } }
        internal static double m2km { get { return _m2km; } }
        internal static double Deg2rad { get { return _toRad; } }

        private const double _radiusEarthMiles = 3959;
        private const double _radiusEarthKM = 6371;
        private const double _m2km = 1.60934;
        private const double _toRad = Math.PI / 180;

        /// <summary>
        /// Distance between two geographic points on surface, km/miles
        /// Haversine formula to calculate
        /// great-circle (orthodromic) distance on Earth
        /// High Accuracy, Medium speed
        /// re: http://en.wikipedia.org/wiki/Haversine_formula
        /// </summary>
        /// <param name="Lat1">double: 1st point Latitude</param>
        /// <param name="Lon1">double: 1st point Longitude</param>
        /// <param name="Lat2">double: 2nd point Latitude</param>
        /// <param name="Lon2">double: 2nd point Longitude</param>
        /// <returns>double: distance, km/miles</returns>
        public static double DistanceHaversine(double Lat1,
                                                    double Lon1,
                                                    double Lat2,
                                                    double Lon2,
                                                    UnitSystem UnitSys)
        {
            try
            {
                double _radLat1 = Lat1 * _toRad;
                double _radLat2 = Lat2 * _toRad;
                double _dLatHalf = (_radLat2 - _radLat1) / 2;
                double _dLonHalf = Math.PI * (Lon2 - Lon1) / 360;

                // intermediate result
                double _a = Math.Sin(_dLatHalf);
                _a *= _a;

                // intermediate result
                double _b = Math.Sin(_dLonHalf);
                _b *= _b * Math.Cos(_radLat1) * Math.Cos(_radLat2);

                // central angle, aka arc segment angular distance
                double _centralAngle = 2 * Math.Atan2(Math.Sqrt(_a + _b), Math.Sqrt(1 - _a - _b));

                // great-circle (orthodromic) distance on Earth between 2 points
                if (UnitSys == UnitSystem.SI) { return _radiusEarthKM * _centralAngle; }
                else { return _radiusEarthMiles * _centralAngle; }
            }
            catch { throw; }
        }

        /// <summary>
        /// Distance between two geographic points on surface, km/miles
        /// Spherical Law of Cosines formula to calculate
        /// great-circle (orthodromic) distance on Earth;
        /// High Accuracy, Medium speed
        /// re: http://en.wikipedia.org/wiki/Spherical_law_of_cosines
        /// </summary>
        /// <param name="Lat1">double: 1st point Latitude</param>
        /// <param name="Lon1">double: 1st point Longitude</param>
        /// <param name="Lat2">double: 2nd point Latitude</param>
        /// <param name="Lon2">double: 2nd point Longitude</param>
        /// <returns>double: distance, km/miles</returns>
        public static double DistanceSLC(double Lat1,
                                        double Lon1,
                                        double Lat2,
                                        double Lon2,
                                        UnitSystem UnitSys)
        {
            try
            {
                double _radLat1 = Lat1 * _toRad;
                double _radLat2 = Lat2 * _toRad;
                double _radLon1 = Lon1 * _toRad;
                double _radLon2 = Lon2 * _toRad;

                // central angle, aka arc segment angular distance
                double _centralAngle = Math.Acos(Math.Sin(_radLat1) * Math.Sin(_radLat2) +
                        Math.Cos(_radLat1) * Math.Cos(_radLat2) * Math.Cos(_radLon2 - _radLon1));

                // great-circle (orthodromic) distance on Earth between 2 points
                if (UnitSys == UnitSystem.SI) { return _radiusEarthKM * _centralAngle; }
                else { return _radiusEarthMiles * _centralAngle; }
            }
            catch { throw; }
        }

        /// <summary>
        /// Distance between two geographic points on surface, km/miles
        /// Spherical Earth projection to a plane formula (using Pythagorean Theorem)
        /// to calculate great-circle (orthodromic) distance on Earth.
        /// central angle =
        /// Sqrt((_radLat2 - _radLat1)^2 + (Cos((_radLat1 + _radLat2)/2) * (Lon2 - Lon1))^2)
        /// Medium Accuracy, Fast,
        /// relative error less than 0.1% in search area smaller than 250 miles
        /// re: http://en.wikipedia.org/wiki/Geographical_distance
        /// </summary>
        /// <param name="Lat1">double: 1st point Latitude</param>
        /// <param name="Lon1">double: 1st point Longitude</param>
        /// <param name="Lat2">double: 2nd point Latitude</param>
        /// <param name="Lon2">double: 2nd point Longitude</param>
        /// <returns>double: distance, km/miles</returns>
        public static double DistanceSEP(double Lat1,
                                        double Lon1,
                                        double Lat2,
                                        double Lon2,
                                        UnitSystem UnitSys)
        {
            try
            {
                double _radLat1 = Lat1 * _toRad;
                double _radLat2 = Lat2 * _toRad;
                double _dLat = (_radLat2 - _radLat1);
                double _dLon = (Lon2 - Lon1) * _toRad;

                double _a = (_dLon) * Math.Cos((_radLat1 + _radLat2) / 2);

                // central angle, aka arc segment angular distance
                double _centralAngle = Math.Sqrt(_a * _a + _dLat * _dLat);

                // great-circle (orthodromic) distance on Earth between 2 points
                if (UnitSys == UnitSystem.SI) { return _radiusEarthKM * _centralAngle; }
                else { return _radiusEarthMiles * _centralAngle; }
            }
            catch { throw; }
        }
    }
}