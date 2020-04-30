using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bomberman.Api;

namespace MyStrategy
{
    public class BombManager
    {
        public static int BoomRadius = 3;
        public static int BombsCount;

        public static Dictionary<Point, int> myBomb = new Dictionary<Point, int>();

        private HashSet<Point> Exist = new HashSet<Point>();
        private Dictionary<Point, Bomb> Storage = new Dictionary<Point, Bomb>();
        private Dictionary<Point, int> boomArea = new Dictionary<Point, int>();
        private Dictionary<Point, int> testBoomArea = new Dictionary<Point, int>();
        private Dictionary<Point, int> boomAreaChance = new Dictionary<Point, int>(); //TODO
        private Board board;

        public void Update(Board board)
        {
            this.board = board;
            board.BoardForeach(v => {
                switch (board.GetAt(v))
                {
                    case Element.OTHER_BOMB_BOMBERMAN: CreateBomb(v, 3); break;
                    case Element.BOMB_BOMBERMAN: CreateBomb(v, 2); break;
                    case Element.BOMB_TIMER_1: CreateBomb(v, 1); break;
                    case Element.BOMB_TIMER_2: CreateBomb(v, 2); break;
                    case Element.BOMB_TIMER_3: CreateBomb(v, 3); break;
                    case Element.BOMB_TIMER_4: CreateBomb(v, 4); break;
                    case Element.BOMB_TIMER_5: CreateBomb(v, 5); break;
                    default:
                        if (Storage.ContainsKey(v))
                            Exist.Remove(v);
                        break;
                }
            });

            foreach(var my in myBomb.Keys.ToArray())
            {
                if (!Exist.Contains(my))
                {
                    myBomb.Remove(my);
                    continue;
                }
                myBomb[my]--;
                if (myBomb[my] <= 0)
                    myBomb.Remove(my);
            }

            BoomAreaInit();
            testBoomArea.Clear();
            //LeeAlgorithm.PaintMap(boomArea.Keys, p => boomArea.ContainsKey(p) ? "!" : ((char)board.GetAt(p)).ToString());
        }

        public void MarkMyBomb(Point point)
        {
            var bomb = GetBomb(point);
            if (bomb is null) return;
            myBomb[bomb.Position] = bomb.Time + 1;
        }

        public bool IsMyBomb(Point point)
        {
            return myBomb.ContainsKey(point);
        }

        private void BoomAreaInit()
        {
            var bombVisited = new HashSet<Point>();
            boomArea.Clear();
            var bombVisitedInLocalBoom = new HashSet<Point>();
            var localBoomTime = 99;
            foreach (var bombPosition in Exist)
            {
                if (bombVisited.Contains(bombPosition))
                    continue;
                bombVisitedInLocalBoom.Clear();
                localBoomTime = 99;
                BoomAreaSearch(bombPosition);
                foreach (var bomb in bombVisitedInLocalBoom.Select(GetBomb))
                    AppendBoomAreaWithBomb(bomb, localBoomTime);
            }

            void BoomAreaSearch(Point bombPosition)
            {
                var bomb = GetBomb(bombPosition);
                if (bomb is null || boomArea.ContainsKey(bombPosition))
                    return;

                bombVisited.Add(bombPosition);
                bombVisitedInLocalBoom.Add(bombPosition);
                if (bomb.Time < localBoomTime)
                    localBoomTime = bomb.Time;

                var otherBombs = bomb.GetOtherBombInBoomArea();
                if (otherBombs.Any())
                {
                    foreach (var el in otherBombs)
                    {
                        if (!bombVisitedInLocalBoom.Contains(el))
                        {
                            boomArea.Remove(el);
                            BoomAreaSearch(el);
                        }
                    }
                }
            }
        }

        private void AppendBoomAreaWithBomb(Bomb bomb, int time)
        {
            foreach (var point in bomb.GetBoomArea())
                if (boomArea.ContainsKey(point) && boomArea[point] < time)
                    continue;
                else
                    boomArea[point] = time;
        }

        public void CreateTestBomb(Point position, int time) //Пока только поддерживает одну бомбу
        {
            CreateBomb(position, time);
            var bomb = GetBomb(position);
            if (BoomAreaRange(position) < time)
                time = BoomAreaRange(position);
            foreach (var point in bomb.GetBoomArea())
                if (boomArea.ContainsKey(point) && boomArea[point] < time)
                    testBoomArea[point] = boomArea[point];
                else
                    testBoomArea[point] = time;
        }

        public void RemoveTestBomb(Point point)
        {
            Exist.Remove(point);
            testBoomArea.Clear();
        }

        private Bomb CreateBomb(Point position, int time)
        {
            Bomb bomb = null;
            if (Exist.Count <= Storage.Count)
            {
                bomb = new Bomb(position, time);
                Exist.Add(position);
            }
            else
            {
                var oldPosition = Exist.First();
                Exist.Remove(oldPosition);
                Exist.Add(position);
                bomb = Storage[oldPosition];
                bomb.Position = position;
                bomb.Time = time;
            }
            Storage[position] = bomb;
            return bomb;
        }

        public Bomb GetBomb(Point point)
        {
            if (IsBomb(point))
                return Storage[point];
            return null;
        }

        public IEnumerable<Bomb> GetAllBombs(int time)
        {
            foreach(var bombPoint in Exist)
            {
                if (Storage[bombPoint].Time == time)
                    yield return Storage[bombPoint];
            }
        }

        public bool IsBomb(Point point)
        {
            return Exist.Contains(point) && Storage.ContainsKey(point);
        }

        public bool IsBoomArea(Point point, int time = 1)
        {
            return BoomAreaRange(point) <= time;
        }

        public int BoomAreaRange(Point point)
        {
            if (boomArea.ContainsKey(point))
                return boomArea[point];
            if (testBoomArea.ContainsKey(point))
                return testBoomArea[point];
            return 99;
        }

        public IEnumerable<Point> GetSimpleBoomArea(Point bombPosition)
        {
            yield return bombPosition;

            var boomArea = Game.Directions
                .SelectMany(dir => GetBoomPointsInDirection(bombPosition, dir));

            foreach (var point in boomArea)
                yield return point;
        }

        public IEnumerable<Point> GetBoomPointsInDirection(Point bombPosition, Direction direction)
        {
            var start = bombPosition;
            for (var i = 0; i < BoomRadius; i++)
            {
                start = start.Shift(direction);
                if (start.IsElementAs(Elements.FreeForBoom))
                    yield return start;
                if (start.IsElementAs(Elements.DestroedStatic))
                {
                    yield return start;
                    yield break;
                }
                else
                    yield break;
            }
        }

        public class Bomb
        {
            public int Time;
            public Point Position;

            public Bomb(Point position, int time)
            {
                Position = position;
                Time = time;
            }

            public IEnumerable<Point> GetBoomArea()
            {
                yield return Position;

                var boomArea = Game.Directions
                    .SelectMany(dir => Game.GetPointsToDirection(Position, dir, BoomRadius));

                foreach (var point in boomArea)
                    yield return point;
            }

            public IEnumerable<Point> GetOtherBombInBoomArea()
            {
                var boomArea = Game.Directions
                    .SelectMany(dir => Game.GetPointsToDirection(Position, dir, BoomRadius));

                return boomArea.Where(v => v.IsElementAs(Elements.Bombs));
            }
        }
    }
}
