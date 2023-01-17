using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.VisualScripting;
using Random = System.Random;

namespace Script
{
    public class Wfc
    {

        public Vector2Int Size;
        public TileData<Tile>[] Map;
        public PriorityQueue<Vector2Int,int> PriorityQueue;
        public Dictionary<string, TileRelationship> Rules;

        private Dictionary<string, int> _tileDict;

        private bool[,,] _states;

        private bool[,] _colapsed;

        private int _colapsedCount = 0;

        public Wfc(Dictionary<string, TileRelationship> rules, Vector2Int size, TileData<Tile>[] map)
        {
            this.Rules = rules;
            this.Size = size;
            this.Map = map;

            PriorityQueue = new PriorityQueue<Vector2Int, int>();
            if (map.Length != size.x * size.y)
            {
                throw new ArgumentException("Map length do not match with size");
            }

            _tileDict = new Dictionary<string, int>();
            var counter = 0;
            foreach (var uniqTile in map.DistinctBy(e=>e.Hash))
            {
                _tileDict[uniqTile.Hash] = counter;
                counter++;
            }
            _states = new bool[size.x,size.y,_tileDict.Count];
            _colapsed = new bool[size.x, size.y];

            foreach (var tileData in map)
            {
                if (tileData.Position.x == 16 && tileData.Position.y == 16)
                {
                    Collapse(tileData.Position.x,tileData.Position.y,tileData.Hash);
                }
                // var mask = new []{tileDict[tileData.Hash]} ;
                // SetTrueExcept(ref states,tileData.Position.x,tileData.Position.y,mask);
                // colapsed[tileData.Position.x, tileData.Position.y] = true;
                // colapsedCount++;
                
            }
        }

        public bool CollapseNextState()
        {
            while (PriorityQueue.Count > 0)
            {
                var element = PriorityQueue.Dequeue();
                if (!_colapsed[element.x,element.y])
                {
                    Collapse(element.x,element.y);
                    return true;
                }
                
            }

            return false;
        }

        public void WriteToFile(out string[,]finalMap)
        {
            finalMap = new string[_states.GetLength(0), _states.GetLength(1)];
            for (int x = 0; x < _states.GetLength(0); x++)
            {
                for (int y = 0; y < _states.GetLength(1); y++)
                {
                    for (int i = 0; i < _states.GetLength(2); i++)
                    {
                        if (CountState(x,y,false) == 0)
                        {
                            Debug.Log($"x:{x}_y:{y}");
                            break;
                        }
                        if (!_states[x,y,i])
                        {
                            finalMap[x, y] = _tileDict.First(e => e.Value == i).Key;
                        }
                    }
                }
            }
            
            var json = JsonConvert.SerializeObject(finalMap);
            using (FileStream fs = new FileStream("Assets/State.json",FileMode.Create))
            {
                using (StreamWriter streamWriter = new StreamWriter(fs))
                {
                    streamWriter.Write(JsonConvert.SerializeObject(json,new JsonSerializerSettings(){ReferenceLoopHandling = ReferenceLoopHandling.Ignore, Formatting = Formatting.Indented}));
                }
                
            }
        }

        private void Collapse(int x,int y,string value = null)
        {
            if (_colapsed[x,y])
            {
                return;
            }

            var finalTile = value ?? Reduce(x, y);


            // var tile = map[TileUtils.MapIndex(x, y, size.x)];
            var rule = Rules[finalTile];
            SetTrueExcept(ref _states,x,y,new []{_tileDict[finalTile]});
            _colapsedCount++;
            _colapsed[x, y] = true;
            var neighbors = TileUtils.GetNeighbors(Map, x, y, Size.x, Size.y);

            

            foreach (var dir in TileUtils.Dirs)
            {
                if (neighbors[dir] is null)
                {
                    continue;
                }

                var mask = rule.Rules[dir].Keys.Select(e=>_tileDict[e]);
                SetTrueExcept(ref _states,neighbors[dir].Position.x,neighbors[dir].Position.y,mask);
                PriorityQueue.Enqueue(new Vector2Int(neighbors[dir].Position.x,neighbors[dir].Position.y),CountState(x,y));
            }


        }

        private int CountState(int x, int y, bool state = false)
        {
            int count = 0;
            for (int i = 0; i < _states.GetLength(2); i++)
            {
                if (_states[x,y,i] == state)
                {
                    count++;
                }
            }

            return count;
        }

        private void SetTrueExcept(ref bool[,,] array, int x, int y, IEnumerable<int> mask)
        {
            var maskArray = mask as int[] ?? mask.ToArray();
            for (int i = 0; i < array.GetLength(2); i++)
            {
                if (maskArray.Any(e=> e == i))
                {
                    continue;
                }

                array[x, y, i] = true;

            }
        }

        public string Reduce(int x,int y)
        {
            var posibility = new List<int>();
            var max = Int32.MinValue;
            for (int i = 0; i < _states.GetLength(2); i++)
            {
                if (_states[x,y,i])
                {
                    continue;
                }
                posibility.Add(i);
                // max = max > 
            }

            if (posibility.Count <= 0)
            {
                throw new InvalidOperationException("Restart");
                return null;
            }

            var index = new Random(DateTime.Now.GetHashCode()).Next(posibility.Count);
            return _tileDict.First(e => e.Value == posibility[index]).Key;
        }
    }
}