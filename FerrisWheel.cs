using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Location;

namespace Rides.Wheels.FerrisWheel
{
    /// <summary>
    /// Locations are represented in Cortesian plane where x is ground distance
    /// and Y is height. All in respect to center of wheel as origin
    /// </summary>
    public class FerrisWheel : IWheelRides
    {
        /// <summary>
        /// Radius in meters
        /// </summary>
        private double radius;

        /// <summary>
        /// The height of the base in meters.
        /// Distance from ground base center to origin will be sum of this and radius.
        /// </summary>
        private double baseHeight;

        /// <summary>
        /// number of cabins
        /// </summary>
        private int cabinCount;
        /// <summary>
        /// Geocoordinate of the base center on the ground.
        /// </summary>
        private GeoCoordinate baseGeoCoordinate;

        /// <summary>
        /// Coordinate of ground base center in cartesian plane with center of wheel as origin.
        /// </summary>
        private Coordinate normalizedBaseCoordinate;

        /// <summary>
        /// List of cabins on the wheel
        /// </summary>
        private Dictionary<int, Cabin> Cabins { get; set; }

        /// <summary>
        /// Last start time began moving
        /// </summary>
        private DateTime startTime;

        /// <summary>
        /// last stop time
        /// </summary>
        private DateTime stopTime;

        /// <summary>
        /// Speed of the wheel.
        /// Seconds it takes to do one full revolution.
        /// </summary>
        public double SecondsPerRevolution { get; set; }

        /// <summary>
        /// Current state of the wheel.
        /// </summary>
        public State CurrentState { get; private set; }

        /// <summary>
        /// the ctor.
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="baseHeight"></param>
        /// <param name="baseCoordinate"></param>
        /// <param name="numberOfCabins"></param>
        public FerrisWheel(double radius, double baseHeight, GeoCoordinate baseCoordinate, int numberOfCabins)
        {
            this.radius = radius;
            this.baseHeight = baseHeight;
            this.cabinCount = numberOfCabins;
            this.baseGeoCoordinate = baseCoordinate;
            this.Init();
        }

        /// <summary>
        /// Begins moving the cabins. Each cabin on own thread
        /// Record the time
        /// </summary>
        public bool Start()
        {
            this.startTime = DateTime.UtcNow;
            foreach(Cabin cabin in this.Cabins.Values)
            {
                if (!cabin.Start())
                {
                    return false;
                }
            }
            this.CurrentState = State.RUNNING;
            return true;
        }

        /// <summary>
        /// Stops moving the cabins. Record the stop time.
        /// </summary>
        public bool Stop()
        {
            this.stopTime = DateTime.UtcNow;
            foreach(Cabin cabin in this.Cabins.Values)
            {
                if (!cabin.Stop())
                {
                    return false;
                }
            }
            this.CurrentState = State.STOPPED;
            return true;
        }

        /// <summary>
        /// Get the distance from target point on ground to cabin.
        /// Altitude/height assumed to be same as base of wheel.
        /// </summary>
        /// <param name="cabin">Cabin to calculate the distance to</param>
        /// <param name="distanceToWheel">Ground distance from target to cabin </param>
        /// <returns>Distance in meters </returns>
        public double getDistance(Cabin cabin, double groundDistance)
        {
            if (cabin.Location == null)
            {
                return -1;
            }
            var x = groundDistance;
            // altitude assumed to be same as the base
            var y = cabin.Location.Y - normalizedBaseCoordinate.Y;
            return Utilities.Pythagorean(x, y);
        }

        /// <summary>
        /// Get the distance from the target point to cabin.
        /// </summary>
        /// <param name="cabin">
        /// Cabin calculating distance to 
        /// </param>
        /// <param name="distanceToWheel">
        /// Ground istance from target cabin 
        /// </param>
        /// <param name="altitude">
        /// altitude of target point
        /// </param>
        /// <returns></returns>
        public double getDistance(Cabin cabin, double groundDistance, double altitude)
        {
            if (cabin.Location == null)
            {
                return -1;
            }
            var x = groundDistance;
            var y = cabin.Location.Y - this.altitudeToYCoordinate(altitude);
            return Utilities.Pythagorean(x, y);
        }

        /// <summary>
        /// Calculates the distance the wheel has traveled since it began moving
        /// until it stopped or until now if still moving.
        /// </summary>
        /// <returns>circular distance traveled in meters </returns>
        public double getDistanceTraveled()
        {
            double secondsEllapsed = 0;
            // the wheel is at a stop state. Calculate distance traveled since last stopped
            if (stopTime > startTime)
            {
                secondsEllapsed =  (stopTime - startTime).TotalSeconds;
            }
            else if (CurrentState == State.RUNNING)
            {   // the wheel is currently moving. Capture current moment and calculate distance
                var currentTime = DateTime.UtcNow;
                secondsEllapsed = (currentTime - startTime).TotalSeconds;
            }
            return this.FindArcLenght(secondsEllapsed);
            
        }
        
        /// <summary>
        /// Gets the distance the wheel covers after moving for X seconds.
        /// Seconds can be greater than total time wheel spends moving-- 
        /// this would be the expected distance when the wheel moves for that time
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns>distance in meters </returns>
        public double getDistanceTraveled(double seconds)
        {        
            return FindArcLenght(seconds);
        }

        /// <summary>
        /// Get the time in seconds that has passed since we began to move
        /// </summary>
        /// <returns> the time in seconds </returns>
        public double getTotalTimeEllapsed()
        {
            if (stopTime > startTime)
            {
                return (stopTime - startTime).TotalSeconds;
            }
            else if (CurrentState == State.RUNNING)
            {
                return (DateTime.UtcNow - startTime).TotalSeconds;
            }
            return 0;
        }

        /// <summary>
        /// Get the time in seconds required to travel X meters
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public double getTimeForDistance(double distance)
        {
            double radians = distance / radius;
            double speed = Utilities.RevolutionsToAngularSpeed(SecondsPerRevolution);
            double time = radians / speed;
            return time;
        }

        /// <summary>
        /// Locate a cabin by id
        /// </summary>
        /// <param name="id">id of the cabin</param>
        /// <returns>cabin object or null</returns>
        public Cabin FindCabin(int id)
        {
            if (this.Cabins.ContainsKey(id))
            {
                return this.Cabins[id];
            }
            return null;
        }

        /// <summary>
        /// Calculates the distance between any two cabins
        /// </summary>
        /// <param name="cabin1"></param>
        /// <param name="cabin2"></param>
        /// <returns>Distance in meters </returns>
        public double getDistanceBetweenCabins(Cabin cabin1, Cabin cabin2)
        {
            return cabin1.FindDistance(cabin2);
        }

        /// <summary>
        /// Initialize the parameters.
        /// </summary>
        private void Init()
        {
            this.CurrentState = State.STOPPED;
            // place the wheel base on the cartesian plane
            this.normalizedBaseCoordinate = new Coordinate()
            {
                // y will be negative, because we are going down from the center of
                // the wheel to the ground
                X = 0,
                Y = (this.radius + this.baseHeight) * -1
            };
            this.initializeCabins();
        }

        /// <summary>
        /// Initialize the locations of the cabins respective to 
        /// number of cabins and angle between them. First one located at angle 0
        /// </summary>
        private void initializeCabins()
        {
            this.Cabins = new Dictionary<int, Cabin>();
            // set initial location for all the cabins. First one starts at angle 0.
            double radiansBetweenCabins = Math.PI * 2 / this.cabinCount;
            for (int i = 0; i < cabinCount; i++)
            {
                double radians = i * radiansBetweenCabins;
                double x = this.radius * Math.Cos(radians);
                double y = this.radius * Math.Sin(radians);
                Cabin cabin = new Cabin() { Id = i };
                cabin.Location = new Coordinate() { X = x, Y = y };
                cabin.Angle = radians;
                this.Cabins.Add(i, cabin);
            }
        }

        /// <summary>
        /// Calculates the arc length after X seconds
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns>Returns circular distance in meters</returns>
        private double FindArcLenght(double seconds)
        {
            if (seconds > 0 && this.SecondsPerRevolution > 0)
            {
                var speed = Utilities.RevolutionsToAngularSpeed(this.SecondsPerRevolution);
                double radiansTraveled = speed * seconds;
                double arcLength = Utilities.FindArcLength(this.radius, radiansTraveled);
                return arcLength;
            }
            return 0;
        }

        /// <summary>
        /// Place altitude in cortesian plane with wheel center as origin.
        /// </summary>
        /// <param name="altitude">Altitude of point in meters </param>
        /// <returns>The y coordinate altitude is converted/placed in </returns>
        private double altitudeToYCoordinate(double altitude)
        {
            double originAltitude = this.baseGeoCoordinate.Altitude + baseHeight + radius;
            double diff = originAltitude - altitude;
            return diff * -1; // change signs since origin is above ground
        }

        /// <summary>
        /// Update the location of the cabins after x seconds
        /// </summary>
        /// <param name="seconds">seconds that have ellapsed </param>
        private void updateCabinLocations(double seconds)
        {
            double angularSpeed = Utilities.RevolutionsToAngularSpeed(this.SecondsPerRevolution);
            double angleTheta = angularSpeed * seconds;
            foreach(Cabin cabin in this.Cabins.Values)
            {
                cabin.Angle += angleTheta;
                cabin.Location.X =  this.radius * Math.Cos(cabin.Angle);
                cabin.Location.Y = this.radius * Math.Sin(cabin.Angle);
            }
        }
    }
}
