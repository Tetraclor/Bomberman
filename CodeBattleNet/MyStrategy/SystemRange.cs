using Bomberman.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyStrategy
{
    public class TargetWindow
    {
        List<SensorParemetrizer> sensors = new List<SensorParemetrizer>();
        SensValue head;
        SensValue[,] pole;

        public TargetWindow(int size)
        {
            pole = new SensValue[size, size];
            for (var i = 0; i < size; i++)
                for (var j = 0; j < size; j++)
                    pole[i,j] = new SensValue();
            head = new SensValue();
        }

        public Sensor AddSensor(Element element, Func<SensData, int> activate)
        {
            return AddParametrizedSensor(new SensorParemetrizer(() => Game.Board.Get(element), activate));
        }

        public Sensor AddSensor(Func<IEnumerable<Point>> getActivePoints, Func<SensData, int> activate)
        {
            return AddParametrizedSensor(new SensorParemetrizer(getActivePoints, activate));
        }

        public Sensor AddParametrizedSensor(SensorParemetrizer sensor)
        {
            sensor.Sensor = new Sensor(sensor.Activate);
            sensors.Add(sensor);
            return sensor.Sensor;
        }

        public SensValue Update(Point center)
        {
            head = SensValue.Zero;
            foreach (var sensor in sensors)
            {
                if (sensor.active)
                {
                    var elements = sensor.GetActivePoints();
                    var sum = SensValue.Zero;
                    foreach (var el in elements.Select(ap => sensor.Sensor.ActivateAndGetValue(center, ap)))
                    {
                        sum += el;
                    }
                    head += sum;
                }
            }

            return head;
        }

        public class SensorParemetrizer
        {
            public bool active = true;
            public Func<IEnumerable<Point>> GetActivePoints;
            public Func<SensData, int> Activate;
            public Sensor Sensor;

            public SensorParemetrizer(
                Func<IEnumerable<Point>> getActivePoints,
                Func<SensData, int> activate
                )
            {
                GetActivePoints = getActivePoints;
                Activate = activate;
            }
        }

        public class Sensor
        {
            public Func<SensData, int> Activate;
            public Element Element;

            public Sensor(Func<SensData, int> activate)
            {
                Activate = activate;
            }

            public bool IsActiv(Point point)
            {
                return point.IsElementAs(Element);
            }

            public SensValue GetValue(Point center, Point point)
            {
                var Weight = new SensValue();
                if (IsActiv(point))
                {
                    return ActivateAndGetValue(center, point);
                }
                return SensValue.Zero;
            }

            public SensValue ActivateAndGetValue(Point center, Point point)
            {
                var Weight = new SensValue();
                var sensData = new SensData();
                var shift = sensData.Shift = point - center;
                sensData.Point = point;
                sensData.Length = Math.Abs(sensData.Shift.X) + Math.Abs(sensData.Shift.Y);
                if (shift.X < 0)
                {
                    sensData.Direction = Direction.Left;
                    Weight.left = Activate(sensData);
                }
                else if (shift.X > 0)
                {
                    sensData.Direction = Direction.Right;
                    Weight.right = Activate(sensData);
                }
                if (shift.Y > 0)
                {
                    sensData.Direction = Direction.Up;
                    Weight.up = Activate(sensData);
                }
                else if (shift.Y < 0)
                {
                    sensData.Direction = Direction.Down;
                    Weight.down = Activate(sensData);
                }


                return Weight;
            }
        }

        public class SensData
        {
            public Point Point;
            public Point Shift;
            public Direction Direction;
            public int Length;
        }

        public class SensValue
        {
            public static SensValue Zero = new SensValue();

            public int left;
            public int up;
            public int right;
            public int down;

            public SensValue() { }
            public SensValue(int left, int up, int right, int down)
            {
                this.down = down;
                this.up = up;
                this.left = left;
                this.right = right;
            }
            public Direction DirectionOfValue(int value)
            {
                if (value == up) return Direction.Up;
                if (value == right) return Direction.Right;
                if (value == left) return Direction.Left;
                if (value == down) return Direction.Down;
                return Direction.Stop;
            }

            public IEnumerable<Direction> DirectionsPriority()
            {
                var list = new List<int>() { down, up, left, right };
                list.Sort();
                foreach(var value in list)
                {
                    yield return DirectionOfValue(value);
                }
            }

            public static SensValue operator +(SensValue a, SensValue b)
            {
                return new SensValue(a.left + b.left, a.up + b.up, a.right + b.right, a.down + b.down);
            }

            public override string ToString()
            {
                return $"{left} {up} {right} {down}";
            }
        }
    }
}
