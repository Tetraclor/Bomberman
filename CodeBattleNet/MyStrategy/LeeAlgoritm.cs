using Bomberman.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyStrategy
{
    public static class LeeAlgorithm
    { 
        public static Point Finish { get; private set; }
        public static bool IsFinished;
        public static Dictionary<Point, int> map = new Dictionary<Point, int>();
        public static HashSet<Point> visited = new HashSet<Point>();
        public static int valueWave = 0;

        public static IEnumerable<Point> Search(Point start,  Func<Point, bool> isFree, IEnumerable<Point> finishPoints)
        { 
            var map = SpreadWave(start, isFree, finishPoints.Contains);
           //Console.WriteLine(PaintMap(map.Keys, p => LeeMapDisplay(map, p)));
            if (!IsFinished)
                return Array.Empty<Point>();
            var path = RestoringPath(map, start, Finish);
           // Console.WriteLine(PaintMap(path, p => path.Contains(p) ? LeeMapDisplay(map, p) : "   "));
            return path;
        }

        public static IEnumerable<Point> Search(Point start, Func<Point, bool> isFree, Func<Point, bool> isFinish)
        {
            var map = SpreadWave(start, isFree, isFinish);
            //Console.WriteLine(PaintMap(map.Keys, p => LeeMapDisplay(map, p)));
            if (!IsFinished)
                return Array.Empty<Point>();
            var path = RestoringPath(map, start, Finish);
            // Console.WriteLine(PaintMap(path, p => path.Contains(p) ? LeeMapDisplay(map, p) : "   "));
            return path;
        }

        public static Dictionary<Point, int> SpreadWave(Point start, Func<Point, bool> isFree, Func<Point, bool> isFinish)
        {
            IsFinished = false;
            Finish = new Point();
            map.Clear();
            visited.Clear();
            var pool = new Queue<Point>();
            valueWave = 0;
            var countElementWave = 1;

            Visit(start);

            while (pool.Any())
            {
                var cur = pool.Dequeue();
                map[cur] = valueWave;
                countElementWave--;
                if (isFinish(cur))
                {
                    Finish = cur;
                    IsFinished = true;
                    break;
                }
                    
                Visit(cur.ShiftLeft());
                Visit(cur.ShiftTop());
                Visit(cur.ShiftRight());
                Visit(cur.ShiftBottom());

                if (countElementWave == 0)
                {
                    valueWave++;
                    countElementWave = pool.Count;
                }
            }

            return map;

            void Visit(Point next)
            {
                if (visited.Contains(next) || !isFree(next))
                    return;
                visited.Add(next);
                pool.Enqueue(next);
            }
        }

        public static List<Point> RestoringPath(Dictionary<Point, int> map, Point start, Point finish)
        {
            var path = new List<Point>() { finish };
            var pathLen = map[finish];
            
            for(var i = pathLen - 1; i >= 0;  i--)
            {
                if (Check(finish.ShiftLeft(), i)) continue;
                if (Check(finish.ShiftTop(), i)) continue;
                if (Check(finish.ShiftRight(), i)) continue;
                Check(finish.ShiftBottom(), i);
            }

            path.Reverse();
            return path;

            bool Check(Point next, int nextValue)
            {
                if (!map.ContainsKey(next) || map[next] != nextValue)
                    return false;
                finish = next;
                path.Add(next);
                return true;
            }
        }

        public static string PaintMap(IEnumerable<Point> mapPoints, Func<Point, string> display, bool reversAxisY = true)
        { 
            if(reversAxisY)
                return string.Join("\n", ListPointsToMap(mapPoints, display).Reverse().Select(line => string.Concat(line)));
            return string.Join("\n", ListPointsToMap(mapPoints, display).Select(line => string.Concat(line)));
        }

        public static T[][] ListPointsToMap<T>(IEnumerable<Point> mapPoints, Func<Point, T> display)
        {
            if (!mapPoints.Any()) return Array.Empty<T[]>();  
            var minX = mapPoints.Min(p => p.X);
            var maxX = mapPoints.Max(p => p.X);
            var minY = mapPoints.Min(p => p.Y);
            var maxY = mapPoints.Max(p => p.Y);
            var map = new T[maxY - minY + 1][];
            for (var i = minY; i <= maxY; i++)
            {
                map[i-minY] = new T[maxX - minX + 1];
                for (var j = minX; j <= maxX; j++)
                {
                    map[i-minY][j-minX] = display(new Point(j, i));
                }
            }
            return map;
        }

        private static string PaintMapOld(IEnumerable<Point> map, Func<Point, string> display)
        {
            var points = map.ToArray();
            var str = new StringBuilder();
            Array.Sort(points, new PointComparer(PointComparer.CompareX));
            var startX = points[0].X;
            Array.Sort(points, new PointComparer(PointComparer.CompareYX));
            var curY = -1;
            var curX = startX;
            for(var i = 0; i < points.Length;  i++)
            {
                if(curY != points[i].Y)
                {
                    curY++;
                    var temp = points[i].X - startX;
                    str.Append('\n');
                    for (var j = 0; j < temp; j++)
                    {
                        str.Append(display(new Point(startX + i, points[i].X)));
                    }                 
                }
                if(curX != points[i].X)
                {

                }
                curX++;
                str.Append(display(points[i]));
            }
            return str.ToString();
        }

        // Значения до 100. Далее отоборажается неккоректно.
        public static string LeeMapDisplay(Dictionary<Point, int> map, Point point)
        {
            if(map.ContainsKey(point))
            {
                var value = map[point];
                if (value / 10 == 0)
                    return $"  {value}";
                return $" {value}";
            }
            return "   ";
        }

        class PointComparer : IComparer<Point>
        {
            Func<Point, Point, int> compare;

            public PointComparer(Func<Point, Point, int> compare)
            {
                this.compare = compare;
            }

            public int Compare(Point x, Point y) => compare(x, y);

            public static int CompareYX(Point x, Point y)
            {
                if (x.Y == y.Y)
                    return x.X - y.X;
                return x.Y - y.Y;
            }

            public static int CompareXY(Point x, Point y)
            {
                if (x.X == y.X)
                    return x.Y - y.Y;
                return x.X - y.X;
            }

            public static int CompareX(Point x, Point y)
            {
                 return x.X - y.X;
            }

            public static int CompareY(Point x, Point y)
            {
                return x.Y - y.Y;
            }
        }
    }

   
}
