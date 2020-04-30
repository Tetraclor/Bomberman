using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bomberman.Api;
using NUnit.Framework;
using NUnitLite;

namespace MyStrategy
{
    [TestFixture]
    class Tests
    {
        string map;
        Board board;
        List<Point> markedPoints;
        Point[] spacePoints;

        public void SetUp(params char[] marked)
        {
            map = map.Trim().Replace("\r", "");
            markedPoints = new List<Point>();
            foreach (var ch in marked)
                markedPoints.AddRange(GetMarkedPoint(map, ch));
            spacePoints = GetMarkedPoint(map, ' ').ToArray();
            map = ClearMap(map, '+');
            board = new Board(map, true);
            Game.Update(board);
        }

        static string[] maps = { @"
######
#1+++#
#+   #
#+   #
#+   #
######",@"
######
# ++ #
#+12+#
# ++ #
# ++ #
######",@"
######
#+   #
#1@++#
#+   #
#+   #
######", @"
######
#+   #
#1+# #
##   #
#    #
######", @"
######
# &+1#
#   +#
#   +#
#   $#
######",@"
######
#$ $1#
#+# +#
#32+4#
#+# $#
######",
            @"
######
#3++4#
#+  +#
#+  +#
#5+++#
######"


        };
        [TestCase(0, '+', '1')]
        [TestCase(1, '+', '1', '2')]
        [TestCase(2, '+', '1', '@')]
        [TestCase(3, '+')]
        [TestCase(4, '+', '&', '$')]
        [TestCase(5, '+', '$', '1', '2', '3', '4')]
        [TestCase(6, 3, '+', '3', '4', '5')]
        [TestCase(6, 4, '+', '3', '4', '5')]
        [TestCase(6, 5, '+', '3', '4', '5')]
        public void SimpleCheckBombArea(int indexMap, int time = 1, params char[] marked)
        {
            map = maps[indexMap];
            SetUp(marked);
            foreach (var point in markedPoints)
                Assert.AreEqual(true, Game.BombManager.IsBoomArea(point, time));
            foreach (var point in spacePoints)
                Assert.AreEqual(false, Game.BombManager.IsBoomArea(point, time));
        }

        static string[] testMapSimple = { @"
####
#  #
#  #
####",@"
#####
#   #
#   #
#   #
#####
",
        };

        static string[] maps1 = { @"
####
# 1#
#@ #
####",
            @"
####
#1 #[
#@+#
####",
            @"
####
#3@#
# +#
####",     
            @"
#####
#2 1#
#@+ #
#1 2#
#####",
            @"
#####
#   #
#@#1#
#   #
#####", 
            @"
#####
#  1#
#@ 1#
#+  #
#####",     @"
#####
#+ 2#
#@ 1#
#  1#
#####",
        };


        [TestCase(0, '+')]
        [TestCase(1, '@', '+')]
        [TestCase(2, '@', '+')]
        [TestCase(3, '@', '+')]
        [TestCase(4, '+')]
        [TestCase(5, '@', '+')]
        [TestCase(6, '@', '+')]
        public void TestSaveMode(int indexMap, params char[] marked)
        {
            map = maps1[indexMap];
            SetUp(marked);
            var path = Game.SaveMod();
            Assert.AreEqual(path.Count(), markedPoints.Count);

            foreach (var point in markedPoints)
                Assert.IsTrue(path.Contains(point));
        }

        static string[] maps2 = { @"
####
#@ #
#  #
####",   @"
####
#@2#
#  #
####",  @"
####
#@1#
#  #
####",  @"
####
#@ #
# 1#
####",
        @"
####
#@ #
# 2#
####", 
        @"
#####
#1 @#
#   #
#   #
#####
",
        };

        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        [TestCase(3, true)]
        [TestCase(4, true)]
        [TestCase(5, false)]
        public void TestSaveAct(int indexMap, bool isAct)
        {
            map = maps2[indexMap];
            SetUp();
            
        }

        public IEnumerable<Point> GetMarkedPoint(string map, char mark = '+')
        {
            var lines = map.Split('\n').Where(line => !string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line)).ToArray();
            for (var i = 0; i < lines.Length; i++)
            {
                for (var j = 0; j < lines[i].Length; j++)
                {
                    if (lines[i][j] == mark)
                        yield return new Point(j, i);
                }
            }
        }

        public string ClearMap(string map, params char[] chars)
        {
            foreach (var ch in chars)
                map = map.Replace(ch, ' ');
            return map;
        }
    }

    [TestFixture]
    public class GameTest
    {
        
        public void Test()
        {
            
        }
    }

    [TestFixture]
    class LeeAlgoritmShould
    {
        [Test]
        public void FinishMatchWithStart()
        {
            var start = new Point(0, 0);
            var map = LeeAlgorithm.SpreadWave(start, p => true, p => true);
            Assert.IsTrue(map.ContainsKey(start));
            Assert.IsTrue(map[start] == 0);
        }

        [Test]
        public void FinishNearWithStart()
        {
            var start = new Point(0, 0);
            var finish = new Point(1, 0);
            var map = LeeAlgorithm.SpreadWave(start, p => true, p => p == finish);
            Assert.IsTrue(map.Values.Count <= 5);
            Assert.IsTrue(map.ContainsKey(start) && map.ContainsKey(finish));
            Assert.IsTrue(map[start] == 0 && map[finish] == 1);
        }

        [Test]
        public void  NotMoveForbiddenPoints()
        {
            var str = @"
####
#s #
# b#
####
";
            var str2 = @"
#####
#s#f#
#  b# 
#####
";
            MakeMapTest(str, 2, 4);
            MakeMapTest(str2, 3, 4);
        }

        [Test]
        public void MapPainterTest()
        {
            //var result = LeeAlgorithm.PaintMap();
            //throw new Exception(result);
        }


        public void MakeMapTest(string stringMap, int pathLen, int maxCountPointInResultMap = -1)
        {
            var map = CreateMap(stringMap);
            var bestFinish = FindChar(map, 'b').First();
            var finish = FindChar(map, 'f');
            var start = FindChar(map, 's').First();
            var resultMap = LeeAlgorithm.SpreadWave(start, p => map[p.Y][p.X] != '#', p => finish.Contains(p) || bestFinish == p);
            if (maxCountPointInResultMap != -1)
                Assert.IsTrue(resultMap.Count <= maxCountPointInResultMap);
            Assert.IsTrue(resultMap.ContainsKey(start));

            Assert.IsTrue(resultMap.ContainsKey(bestFinish));
            Assert.IsTrue(resultMap[bestFinish] == pathLen);     
        }

        public string[] CreateMap(string stringMap)
        {
            return stringMap.Split('\n').Where(line => !string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line)).ToArray();
        }

        public IEnumerable<Point> FindChar(string[] map, char ch)
        {
            for (var i = 0; i < map.Length; i++)
            {
                for (var j = 0; j < map[i].Length; j++)
                {
                    if (map[i][j] == ch)
                        yield return new Point(j, i);
                }
            }
        }
    }
}
