/*-
 * #%L
 * Codenjoy - it's a dojo-like platform from developers to developers.
 * %%
 * Copyright (C) 2018 Codenjoy
 * %%
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public
 * License along with this program.  If not, see
 * <http://www.gnu.org/licenses/gpl-3.0.html>.
 * #L%
 */
using Bomberman.Api;
using MyStrategy;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Demo
{
    public class Logger
    {
        private static List<Board> boards = new List<Board>();
        public static string PathMetaFile = "meta.txt";
        public static int IndexFileLog = 0;
        private static int maxTickLog = 10;
        private static char splitter = '`';
        static MySolver game = new MySolver();

        static Logger()
        {
            if(File.Exists(PathMetaFile))
            {
                var text = File.ReadAllText(PathMetaFile);
                IndexFileLog = int.Parse(text);
            }
            else
            {
                IndexFileLog = 0;
                File.AppendAllText(PathMetaFile, "0");
            }
        }

        public static string Get(Board gameBoard)
        {
            boards.Add(gameBoard);
            if(boards.Count == maxTickLog)
            {
                boards.RemoveAt(0);
            }
            if (gameBoard.IsMyBombermanDead)
                WriteLog();

            return game.Get(gameBoard);

        }

        private static void WriteLog()
        {
            var data = string.Join(splitter.ToString(), boards.Select(b => b.BoardAsString()));
            File.WriteAllText($"{IndexFileLog}.log", data);
           
            IndexFileLog++;
            File.WriteAllText(PathMetaFile, IndexFileLog.ToString());
            boards.Clear();
        }

        public static List<Board> Read(int logIndex)
        {
            var path = @"C:\Users\Daniil\Desktop\codebattle-bomberman-clients-2020-master\CodeBattleNet\Demo\bin\Debug\" + logIndex + ".log";
            if (!File.Exists(path)) return new List<Board>();
            var data  = File.ReadAllText(path);
            return data.Split(splitter).Select(v => new Board(v, true)).ToList();
        }
    }
}
