using Bomberman.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MyStrategy.TargetWindow;

namespace MyStrategy
{
    public static class Game
    {
        public static Direction[] Directions = new Direction[] { Direction.Left, Direction.Up, Direction.Right, Direction.Down };
        public static Board Board;
        public static Point HeroPosition;
        public static BombManager BombManager = new BombManager();
        public static bool HeroIsInBoomArea => BombManager.IsBoomArea(HeroPosition);
        public static bool IsAct;
        public static Direction HeroMove = Direction.Stop;
        public static Direction PrevHeroMove = Direction.Stop;

        public static bool PrevMoveFailed => HeroMove == PrevHeroMove && HeroPosition == PrevHeroPosition;
        public static Point ConflictPoint => PrevMoveFailed ? HeroPosition.Shift(PrevHeroMove) : new Point(); 

        public static Point NextHeroPosition => HeroPosition.Shift(HeroMove);
        public static Point PrevHeroPosition;
        public static Point PrevPrevHeroPosition;
        
        public static Point NearEnemy;
        public static bool BombToMove = true;
        public static Point[] DirectLineSightPoints;
        public static Dictionary<Point, Point> ActivePlayrs = new Dictionary<Point, Point>();

        public static Point[] NearPointsFree => HeroPosition.GetNear(Elements.FreeForHero).ToArray();
        public static Point[] NearPointsFreeBoom => NearPointsFree.Where(v => !BombManager.IsBoomArea(v, 1)).ToArray();
        public static Point[] NearPointsChoperDanger => NearPointsFree.Where(v => v.GetNear(Element.MEAT_CHOPPER).Any()).ToArray();
        public static Point[] NeatPointsOtherBomberman => NearPointsFree.Where(v => v.GetNear(Element.OTHER_BOMBERMAN).Any()).ToArray();

        public static List<Point> DangerPoints = new List<Point>();


        static Game()
        {
        
        }

        public static void Update(Board board)
        {
            Board = board;
            DangerPoints.Clear();
            HeroPosition = board.GetBomberman();
            BombManager.Update(board);
            DirectLineSightPoints = GetPointsInDirectLineSight(HeroPosition, 3).ToArray();  
        }

        public static Point FindNearEnemyNextPoint()
        {
            var targetEnemies = Board.GetOtherBombermans();
            targetEnemies = targetEnemies.Where(p => !BombManager.IsBoomArea(p, 5)).ToList();
            var path = LeeAlgorithm.Search(HeroPosition, p => {
                return (p.IsElementAs(Elements.FreeForHero) || targetEnemies.Contains(p)) && !DangerPoints.Contains(p);
            }, targetEnemies);

            if (LeeAlgorithm.IsFinished)
            {
                NearEnemy = LeeAlgorithm.Finish;
            }

            if (path.Count() > 1)
            {
                var next = path.Skip(1).First();
                return next;
            }
            return new Point();
        }

        public static IEnumerable<Point> SaveMod()
        {
            if(BombManager.IsBoomArea(HeroPosition, 5))
            {
                var path = LeeAlgorithm.Search(HeroPosition, 
                    p => {
                        if(BombManager.IsBoomArea(p, 5))
                        {
                            return BombManager.BoomAreaRange(p) != LeeAlgorithm.valueWave;
                        }
                        return p.IsElementAs(Elements.FreeForHero);
                    },
                    p => !BombManager.IsBoomArea(p, 5));
                
                return path;
            }
            return Array.Empty<Point>();
        }

        public static bool IsNextMoveDeadlock(Point next, int minFreePointInZone = 10)
        {
            if (Board.Get(Element.BOMB_BOMBERMAN).Any() || IsAct)
            {
                var countFreeSpace = 0;
                var mapSpreadWave = LeeAlgorithm.SpreadWave(next, p => p.IsElementAs(Elements.FreeForHero),
                    p =>
                    {
                        if (countFreeSpace >= minFreePointInZone)
                            return true;
                        countFreeSpace++;
                        return false;
                    });
                if (mapSpreadWave.Count < minFreePointInZone)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Act()
        {
            var enemy = GetPointsInDirectLineSight(HeroPosition, 4)
                .Filter(Element.OTHER_BOMBERMAN, Element.MEAT_CHOPPER).ToList();

            enemy.AddRange(GetPointsInDirectLineSight(HeroPosition, 2).Filter(Element.DESTROYABLE_WALL).Where(v => !BombManager.IsBoomArea(v, 4)));

            var bombsPoints = DirectLineSightPoints.Filter(Elements.Bombs);
            var bombs = bombsPoints.Select(BombManager.GetBomb);
            var myBomb = bombsPoints.Where(bp => BombManager.IsMyBomb(bp)).OrderBy(v => BombManager.BoomAreaRange(v));
            var otherBombs = bombsPoints.Where(bp => !BombManager.IsMyBomb(bp)).OrderBy(v => BombManager.BoomAreaRange(v));
            if (bombsPoints.Any(bp => !BombManager.IsMyBomb(bp)))
            {
                if (myBomb.Any())
                {
                    var myRange = BombManager.BoomAreaRange(myBomb.First());
                    var otherRange = BombManager.BoomAreaRange(otherBombs.First());
                    if (myRange <= otherRange && myRange > 1)
                        return true;
                }
                return false;
            }

            if (enemy.Any())
            {
                BombToMove = true;
                
                if (BombManager.IsBoomArea(HeroPosition, 5))
                {  
                    if(bombsPoints.Any(b => BombManager.BoomAreaRange(b) < 2))
                        return false;
                    return true;
                }
                return true;
            }

            enemy = Board.GetMeatChoppers();
            enemy.AddRange(Board.GetOtherBombermans());
            if (enemy.Any(v => HeroPosition.Length(v) < 4))
                return true;

            return false;
        }

        public static List<PointData> SortNearPointsWithSaveRange()
        {
            var nears = HeroPosition.GetNear(Elements.FreeForHero).Select(v => new PointData(v)).ToList();
            nears.Sort();
            return nears;
        }

        public static Point Chois(Point desired)
        {
            var nears = Game.HeroPosition.GetNear(Elements.FreeForHero).Select(v => new PointData(v)).ToList();
            nears.Add(new PointData(Game.HeroPosition));
            
          //  MySolver.Log(string.Join("\n", nears.Select(v => v.ToString())));
            var chois = HeroPosition;

       //     if (Repeat(false, false)) return chois;
            if (Repeat(true, false)) return chois;
            if (Repeat(true, true)) return chois;

            if (Game.IsAct && chois == HeroPosition)
            {
            //    MySolver.Log("Если не ставить бомбу");
                Game.IsAct = false;
                BombManager.RemoveTestBomb(HeroPosition);
          //      MySolver.Log(string.Join("\n", nears.Select(v => v.ToString())));

            }
            else
                return HeroPosition;
            nears = Game.HeroPosition.GetNear(Elements.FreeForHero).Select(v => new PointData(v)).ToList();
            nears.Add(new PointData(Game.HeroPosition));

       //     if (Repeat(false, false)) return chois;
            if (Repeat(true, false)) return chois;
            if (Repeat(true, true)) return chois;

            return chois;

            Point RandomOrDesired(IEnumerable<Point> points)
            {
                if (points.Contains(desired))
                    return desired;
                return  RandomSelect(points);
            }

            bool Repeat(bool nearEnemy, bool nearChoper)
            {
                if (PointData.TryGetIsAll(nears, out IEnumerable<Point> points, 4, 2, 1, nearEnemy, nearChoper))
                {
                    chois = RandomOrDesired(points);
                    return true;
                }
                if (PointData.TryGetIsAll(nears, out points, 4, 2, 2, nearEnemy, nearChoper))
                {
                    chois = RandomOrDesired(points);
                    return true;
                }
                if (PointData.TryGetIsAll(nears, out points, 4, 1, 2, nearEnemy, nearChoper))
                {
                    chois = RandomOrDesired(points);
                    return true;
                }
                if (PointData.TryGetIsAll(nears, out points, 3, 2, 1, nearEnemy, nearChoper))
                {
                    chois = RandomOrDesired(points);
                    return true;
                }

                if (PointData.TryGetIsAll(nears, out points, 3, 3, 2, nearEnemy, nearChoper))
                {
                    chois = RandomOrDesired(points);
                    return true;
                }
                if (PointData.TryGetIsAll(nears, out points, 4, 1, 1, nearEnemy, nearChoper))
                {
                    chois = RandomOrDesired(points);
                    return true;
                }
                if (PointData.TryGetIsAll(nears, out points, 4, 2, 3, nearEnemy, nearChoper))
                {
                    chois = RandomOrDesired(points);
                    return true;
                }
                if (PointData.TryGetIsAll(nears, out points, 2, 1, 1, nearEnemy, nearChoper))
                {
                    chois = RandomOrDesired(points);
                    return true;
                }
                return false;
            }
        }

        public class PointData : IComparable<PointData>
        {
            // private static Dictionary<Point, PointData> data = new Dictionary<Point, PointData>();
            public Point Point;
            public int BoomArea = 99;
            public int CountNearFree;
            public int LenPathToNearPointNoBoom;
            public bool IsFree;
            public bool IsEnemy;
            public bool IsNearEnemy;
            public bool IsNearChoper;

            public PointData(Point point)
            {
                Point = point;
                BoomArea = BombManager.BoomAreaRange(point);
                IsFree = point.IsElementAs(Elements.FreeForHero);
                IsEnemy = point.IsElementAs(Element.OTHER_BOMBERMAN);
                IsNearEnemy = point.GetNear(Element.OTHER_BOMBERMAN).Any();
                IsNearChoper = point.GetNear(Element.MEAT_CHOPPER).Any();
                CountNearFree = point.GetNear(Elements.FreeForHero).Where(v => !(v == HeroPosition && Game.IsAct)).Count();
                var path = LeeAlgorithm.Search(point, p => p.IsElementAs(Elements.FreeForHero), 
                    p => {
                        return !BombManager.IsBoomArea(p, 5);
                        });
                LenPathToNearPointNoBoom = path.Count() - 1;
                if (LenPathToNearPointNoBoom == -1)
                    LenPathToNearPointNoBoom = 99;
                // data[point] = this;
            }

            public bool Is(int boomAreaRange, int countNearFree, int lenPathToFree, bool nearEnemy, bool nearChoper)
            {
                return boomAreaRange <= BoomArea && countNearFree <= CountNearFree && LenPathToNearPointNoBoom <= lenPathToFree && (nearEnemy || !IsNearEnemy) && (nearChoper || !IsNearChoper);
            }

            public static bool Any(IEnumerable<Point> points, int boomAreaRange, int countNearFree, int lenPathToFree, bool nearEnemy, bool nearChoper)
            {
                return points.Any(p => new PointData(p).Is(boomAreaRange, countNearFree,lenPathToFree, nearEnemy, nearChoper));
            }

            public static bool TryGetIs(IEnumerable<Point> points, out Point point, int boomAreaRange, int countNearFree, int lenPathToFree, bool nearEnemy, bool nearChoper)
            {
                point = points.FirstOrDefault(p => new PointData(p).Is(boomAreaRange, countNearFree, lenPathToFree, nearEnemy, nearChoper));
                if (point == default)
                    return false;
                return true;
            }

            public static bool TryGetIsAll(IEnumerable<Point> points, out IEnumerable<Point> point, int boomAreaRange, int countNearFree, int lenPathToFree, bool nearEnemy, bool nearChoper)
            {
                point = points.Where(p => new PointData(p).Is(boomAreaRange, countNearFree, lenPathToFree, nearEnemy, nearChoper));
                if (!point.Any())
                    return false;
                return true;
            }

            public static bool TryGetIsAll(IEnumerable<PointData> points, out IEnumerable<Point> point, int boomAreaRange, int countNearFree, int lenPathToFree, bool nearEnemy, bool nearChoper)
            {
                points = points.Where(p => p.Is(boomAreaRange, countNearFree, lenPathToFree, nearEnemy, nearChoper));
                if (!points.Any())
                {
                    point = Array.Empty<Point>();
                    return false;
                }
                point = points.Select(v => v.Point);
                return true;
            }

            public int CompareTo(PointData other)
            {
                if (BoomArea > other.BoomArea)
                    return 1;
                else if (BoomArea < other.BoomArea)
                    return -1;
                if (IsNearEnemy && !other.IsNearEnemy)
                    return 1;
                else if (!IsNearEnemy && other.IsNearEnemy)
                    return -1;
                if (CountNearFree < other.CountNearFree)
                    return 1;
                else if (CountNearFree > other.CountNearFree)
                    return -1;
                if (IsNearChoper && !other.IsNearChoper)
                    return 1;
                else if (!IsNearChoper && other.IsNearChoper)
                    return -1;
                return 0;
            }

            public string ToString()
            {
                return $"{HeroPosition.ToDirection(Point)}: взрыв[{BoomArea}]" +
                       $" свободность[{CountNearFree}] длина безопасности[{LenPathToNearPointNoBoom}] рядом противник[{IsNearEnemy}] рядом чопер[{IsNearChoper}]";
            }
        }

        public static IEnumerable<Point> GetPointsToDirection(Point start, Direction direction, int maxLen)
        {
            for(var i = 0; i < maxLen; i++)
            {
                start = start.Shift(direction);
                if (start.IsElementAs(Element.WALL))
                    yield break;
                if(start.IsElementAs(Elements.DestroedStatic))
                {
                    yield return start;
                    yield break;
                }
                yield return start;
            }
        }

        public static IEnumerable<Point> GetPointsInDirectLineSight(Point start, int maxLen)
        {
            return Game.Directions.SelectMany(dir => GetPointsToDirection(start, dir, maxLen));
        }

        public static void HeroMoveTo(Point point)
        {
            HeroMove = HeroPosition.ToDirectionOrStop(point);
        }

        static Random random = new Random();
        public static T RandomSelect<T>(IEnumerable<T> elements)
        {
            return elements.ElementAt(random.Next(0, elements.Count()));
        }
    }

    public class MySolver
    {
        public static Action<string> Log = Console.WriteLine;

        int countStop = 0;
        
        public string Get(Board board)
        {
            return GetWithLog(board);
        }

        public string GetWithLog(Board board)
        {
            Game.Update(board);
            Game.IsAct = Game.Act();
         //   Log(Game.IsAct ? "Поставить бомбу" : "Не ставить бомбу");
            if (Game.PrevHeroPosition == Game.HeroPosition || Game.PrevPrevHeroPosition == Game.HeroPosition)
            {
                Game.IsAct = true;
            }
            if (Game.IsAct)
                Game.BombManager.CreateTestBomb(Game.HeroPosition, 4);



            var pointToEnemy = Game.FindNearEnemyNextPoint();
            var savePoints = Game.NearPointsFree.Where(v => !Game.BombManager.IsBoomArea(v, 2)).ToList();
            if (!savePoints.Any() && Game.IsAct)
            {
           //     Log("Безопасный путь не найден, отмена постановки бомбы");
                Game.IsAct = false;
                Game.BombManager.RemoveTestBomb(Game.HeroPosition);
                savePoints = Game.NearPointsFree.Where(v => !Game.BombManager.IsBoomArea(v, 2)).ToList();
          //      Log("Безопасный путь " + (savePoints.Any() ? "найден" : "не найден"));
            }
            if (!savePoints.Any())
            {
           //     Log("Строго безопасный путь не найден, поиск менее безопасного пути");
                savePoints = Game.NearPointsFree.Where(v => !Game.BombManager.IsBoomArea(v, 1)).ToList();
            //    Log("Менее безопасный путь " + (savePoints.Any() ? "найден" : "не найден"));
            }
                

            if (savePoints.Contains(pointToEnemy))
            {
                Game.HeroMoveTo(pointToEnemy);
           //     Log($"Найден безопасный путь до противника {Game.HeroMove}");
            } 
            else if (savePoints.Any())
            {
                Game.HeroMoveTo(Game.RandomSelect(savePoints));
            //    Log($"Безопасный путь найден {Game.HeroMove}, но не в направлении к противнику");
            }
            else
            {
           //     Log("Безопасный путь не найден");
            }


            if (Game.HeroMove != Direction.Stop && Game.IsNextMoveDeadlock(Game.NextHeroPosition) && !Game.BombManager.IsBoomArea(Game.NextHeroPosition, 2))
            {
             //   Log($"Выбранный ход {Game.HeroMove}, тупик");
                var free = savePoints.ToList();
                free.Remove(Game.NextHeroPosition);
                if (Game.PrevMoveFailed)
                {
                    free.Remove(Game.ConflictPoint);
                }
                if (free.Any())
                    Game.HeroMoveTo(Game.RandomSelect(free));
            }

            if (Game.PrevHeroPosition == Game.HeroPosition)
            {
                countStop++;
                if (countStop == 1)
                {
               //     Log("Обнаружено застревание, возможен конфилкт с противником за клетку");
                    var other = Game.NearPointsFreeBoom.Where(v => !Game.NeatPointsOtherBomberman.Contains(v));
                    if(other.Any())
                    {
                        var move = Game.RandomSelect(other);
                 //       Log("Конфликт разрешоне ходом в " + move);
                        Game.HeroMoveTo(move);
                    }
                        
                }
                    
                
            }
            else
                countStop = 0;

            
            var chois = Game.Chois(Game.HeroMove != Direction.Stop ? Game.NextHeroPosition : Game.HeroPosition);
           // Log("Стратеги Выбор: " + Game.HeroPosition.ToDirectionOrStop(chois));
            Game.HeroMoveTo(chois);

            if (Game.HeroMove != Direction.Stop && Game.BombManager.IsBoomArea(Game.NextHeroPosition))
            {
                Log($"Выбраный ход {Game.HeroMove} ведет во взрыв ");
                Game.HeroMove = Direction.Stop;
                Game.IsAct = false;
            }


            if(Game.HeroIsInBoomArea && !Game.NearPointsFreeBoom.Any())
            {
             //   Log("Последнее слово, выхода нет, бомба вперед!");
                Game.IsAct = true;
            }

            
            Game.PrevHeroMove = Game.HeroMove;
            Game.PrevPrevHeroPosition = Game.PrevHeroPosition;
            Game.PrevHeroPosition = Game.HeroPosition;

            Log("Мои бомбы:"  + string.Join("\n", BombManager.myBomb.Select(v => $"[{v.Key}]:{v.Value}")));

            if (Game.IsAct)
                Game.BombManager.MarkMyBomb(Game.HeroPosition);

            Log($"Ход {Game.HeroMove}"  + (Game.IsAct ? ", Act" : ""));
            if (Game.BombToMove)
                return (Game.IsAct ? "Act, " : "") + Game.HeroMove.ToString();
            return Game.HeroMove.ToString() + (Game.IsAct ? ", Act" : "");
        }
    }

    public static class PointExtension
    {
        // лево, верх, право, низ
        public static IEnumerable<Bomberman.Api.Point> GetNear(this Bomberman.Api.Point point)
        {
            yield return point.ShiftLeft();
            yield return point.ShiftTop();
            yield return point.ShiftRight();
            yield return point.ShiftBottom();
        }

        public static IEnumerable<Bomberman.Api.Point> GetNear(this Bomberman.Api.Point point, params Bomberman.Api.Element[] preset)
        {
            foreach (var near in point.GetNear())
                if (near.IsElementAs(preset))
                    yield return near;
        }

        // Не проверено
        public static IEnumerable<Bomberman.Api.Point> GetNearOfRadius(this Bomberman.Api.Point point, int radius, bool isKrest, params Bomberman.Api.Element[] preset)
        {
            for(var i = point.Y - radius; i < point.Y + radius; i++)
            {
                var endJ = 2 * radius - Math.Abs(point.X - i);
                for (var j = Math.Abs(point.X - i); j < endJ; j++)
                    yield return new Bomberman.Api.Point(j, i);
            }
        }

        public static IEnumerable<Bomberman.Api.Point> Filter(this IEnumerable<Bomberman.Api.Point> points, params Bomberman.Api.Element[] preset)
        {
            return points.Where(v => v.IsElementAs(preset));
        }

        public static Element ToElement(this Point point)
        {
            var ans = Game.Board.GetAt(point);
            return ans;
        }
        public static bool IsElementAs(this Bomberman.Api.Point point, params Bomberman.Api.Element[] preset) => preset.Contains(point.ToElement());

        public static bool IsElementAs(this IEnumerable<Bomberman.Api.Point> point, params Bomberman.Api.Element[] preset) => point.Any(v => v.IsElementAs(preset));

        // Можно прямо здесь зашить невозможность создать направление в стенку. 
        public static Direction ToDirection(this Bomberman.Api.Point for_, Bomberman.Api.Point to)
        {
            if (for_.ShiftLeft() == to) return Direction.Left;
            if (for_.ShiftTop() == to) return Direction.Up;
            if (for_.ShiftRight() == to) return Direction.Right;
            if (for_.ShiftBottom() == to) return Direction.Down;
            return Direction.Stop;
        }

        public static Direction ToDirection(this Bomberman.Api.Point for_, Bomberman.Api.Point to, params Bomberman.Api.Element[] preset)
        {
            if (to.IsElementAs(preset))
                return for_.ToDirection(to);
            return Direction.Stop;
        }

        public static Direction ToDirectionOrStop(this Bomberman.Api.Point for_, Bomberman.Api.Point to)
        {
            if (for_ == to)
                return Direction.Stop;
            else
                return for_.ToDirection(to);
        }

        public static Bomberman.Api.Point Shift(this Bomberman.Api.Point point, Direction direction, int delta = 1)
        {
            switch (direction)
            {
                case Direction.Left: return point.ShiftLeft(delta);
                case Direction.Up: return point.ShiftTop(delta);
                case Direction.Right: return point.ShiftRight(delta);
                case Direction.Down: return point.ShiftBottom(delta);
            }
            throw new ArgumentException("Передано неверное направление");
        }

        public static int Length(this Point a, Point b)
        {
            return Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y);
        }

        private static LengthToXY LengthXY;

        public static void BoardForeach(this Board board, Action<Point> action)
        {
            LengthXY = new LengthToXY(board.BoardSize);
            var BoardSize = board.BoardSize;
            for (int i = 0; i < BoardSize * BoardSize; i++)
            {
                Point pt = LengthXY.GetXY(i);
                action(pt);
            }
        }
    }
}
