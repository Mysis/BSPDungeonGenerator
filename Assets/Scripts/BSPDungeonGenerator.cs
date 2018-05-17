using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using ProceduralGeneration;
using ProceduralGeneration.BSPGenerator;

namespace ProceduralGeneration.BSPDungeonGenerator {

    [System.Serializable]
    public class BSPSettings {
        public int width;
        public int height;
        public int minCellWidth;
        public int minCellHeight;
        public bool useMaxDepth;
        public int maxDepth;

        /*
        public BSPSettings(int width, int height, int minCellWidth, int minCellHeight) {
            this.width = width;
            this.height = height;
            this.minCellWidth = minCellWidth;
            this.minCellHeight = minCellHeight;
        }
        */
    }

    [System.Serializable]
    public class RoomSettings {
        public IntRange numberOfShapes; //maximum number of shapes that a room can consist of
        public int minRoomsForCombine; //this is not the minimum number of rooms, this is the number at which room combining will stop
        public int maxRooms; //will always combine partitions if above max rooms

        public RectangleSettings rectangleSettings;
        [System.Serializable]
        public class RectangleSettings {
            public IntRange width;
            public IntRange height;
            public bool usePartitionAsMax; //use the size of the partition to determine the room's maximum width and height
            public int border;
        }
    }

    [System.Serializable]
    public class ConnectionSettings {
        public IntRange width; //width of the passages
    }

    public class BSPDungeon {
        public BSPSettings bspSettings { get; set; }
        public RoomSettings roomSettings { get; set; }
        public ConnectionSettings connectionSettings { get; set; }
        public Partition mainPartition { get; set; }

        public BSPDungeon(BSPSettings bspSettings, RoomSettings roomSettings, ConnectionSettings connectionSettings) {
            this.bspSettings = bspSettings;
            this.roomSettings = roomSettings;
            this.connectionSettings = connectionSettings;
        }

        public void GenerateDungeon() {
            mainPartition = new Partition(Vector2Int.zero, new Vector2Int(bspSettings.width, bspSettings.height), 0, bspSettings, roomSettings, connectionSettings);
            mainPartition.Split();

            CombinePartitions(roomSettings.minRoomsForCombine, roomSettings.maxRooms); //combine some final partitions to smooth the number of rooms
            CreateRooms();
            CreateConnections();
        }

        private void CombinePartitions(int min, int max) {
            max = Globals.rand.Next(min, max + 1);
            List<Partition> finalPartitions = GenerateFinalPartitionList();
            while (finalPartitions.Count > max) {
                Partition toCombine = null;
                int leastArea = -1; //always combine the smallest partition
                foreach (Partition finalParentPartition in GenerateFinalParentPartitionList()) {
                    if (finalParentPartition.Area < leastArea || leastArea == -1) {
                        leastArea = finalParentPartition.Area;
                        toCombine = finalParentPartition;
                    }
                }
                toCombine.end = toCombine.partitions[1].end;
                toCombine.partitions = null;
                toCombine.isFinal = true;
                finalPartitions = GenerateFinalParentPartitionList();
            }
        }

        private void CreateRooms() {
            foreach (Partition finalPartition in GenerateFinalPartitionList()) {
                finalPartition.GenerateRoom();
            }
        }

        private void CreateConnections() {
            mainPartition.GenerateConnections();
        }

        //starts the recursion on the main partition
        public List<Partition> GeneratePartitionList() {
            return mainPartition.IterateThroughAllPartitions();
        }
        public List<Partition> GenerateFinalPartitionList() {
            return mainPartition.IterateThroughFinalPartitions();
        }
        public List<Partition> GenerateFinalParentPartitionList() {
            return mainPartition.IterateThroughFinalParentPartitions();
        }
    }

    public class Partition {
        public Vector2Int start { get; set; }
        public Vector2Int end { get; set; }
        public int depth { get; set; }
        public Partition[] partitions { get; set; }
        public bool isFinal { get; set; } //does this room have partitions within it (true means it doesn't)

        public List<Shape> room { get; set; } //shapes that the room of the partition consists of. parent partitions don't have shapes for room
        public List<Shape> connection { get; set; } //shapes that connect the two child partitions. final partitions don't have shapes for connection

        private BSPSettings bspSettings { get; set; }
        private RoomSettings roomSettings { get; set; }
        private ConnectionSettings connectionSettings { get; set; }
        private Vector2Int splitSide { get; set; } //always Vector2Int.up or Vector2Int.right (or null)

        public Partition(Vector2Int start, Vector2Int end, int depth, BSPSettings bspSettings, RoomSettings roomSettings, ConnectionSettings connectionSettings) {
            this.start = start;
            this.end = end;
            this.depth = depth;
            this.bspSettings = bspSettings;
            this.roomSettings = roomSettings;
            this.connectionSettings = connectionSettings;

            connection = new List<Shape>();

            isFinal = false;
        }

        public int Width {
            get {
                return end.x - start.x;
            }
        }
        public int Height {
            get {
                return end.y - start.y;
            }
        }
        public int Area {
            get {
                return Width * Height;
            }
        }
        public List<Shape> Shapes {
            get {
                if (isFinal) {
                    return room;
                } else {
                    return partitions[0].Shapes.Concat(partitions[1].Shapes).ToList().Concat(connection).ToList();
                }
            }
        }

        public IntRange GetRange(Vector2Int side) {
            int min = -1;
            int max = -1;
            if (isFinal) {
                foreach (Shape shape in Shapes) {
                    IntRange range = shape.GetRange(side);
                    min = min > range.min || min == -1 ? range.min : min;
                    max = max < range.max || max == -1 ? range.max : max;
                }
            } else {
                foreach (Partition partition in partitions) {
                    int possibleMin = partitions[0].GetRange(side).min;
                    int possibleMax = partitions[1].GetRange(side).max;
                    min = min > possibleMin || min == -1 ? possibleMin : min;
                    max = max < possibleMax || max == -1? possibleMax : max;
                }
            }
            return new IntRange(min, max);
        }

        /*
        public Dictionary<int, Shape> GetShapesForSide(Vector2Int side) {
            Dictionary<int, Shape> shapesForSide = new Dictionary<int, Shape>();
            IntRange totalRange = TotalRange(side);
            for (int i = totalRange.min; i <= totalRange.max; i++) {

            }
        }
        
        /*
        public ValidRange GetValidRange(Vector2Int side) {
            if (isFinal) {
                List<int> invalid = new List<int>();
                foreach (Shape shape in Shapes) {
                    invalid.AddRange(shape.GetRange(side).ToArray());
                }
                IntRange range = new IntRange(invalid.Min(), invalid.Max());
                invalid.RemoveAll(v => v == range.min || v == range.max);
                return new ValidRange(range, invalid.ToArray());
            } else {
                return new ValidRange(partitions[0].GetValidRange(splitAxis).Union(partitions[1].GetValidRange(splitAxis)));
            }
        }

        public int LocationRequiredToConnect(Vector2Int side, IntRange connectFrom) {
            if (!GetValidRange(side).range.WithinRange(connectFrom)) throw new ArgumentOutOfRangeException();
            List<Shape> possibleShapes = new List<Shape>();
            Shape relevantShape;
            foreach (Shape shape in Shapes) {
                if (shape.GetRange(side).WithinRange(connectFrom)) {
                    if (side == Vector2Int.up) {

                    }
                }
            }
            foreach (Shape shape in possibleShapes) {

            }
        }
        */

        //recursively split partitions
        public void Split() {
            if (!bspSettings.useMaxDepth || depth < bspSettings.maxDepth) {
                bool splitX = Width >= bspSettings.minCellWidth * 2;
                bool splitY = Height >= bspSettings.minCellHeight * 2;
                if (splitX && splitY) {
                    if (Globals.rand.Next(0, 2) == 0) {
                        SplitX();
                    } else {
                        SplitY();
                    }
                } else if (splitX) {
                    SplitX();
                } else if (splitY) {
                    SplitY();
                } else {
                    isFinal = true;
                }
            } else {
                isFinal = true;
            }
        }
        private void SplitX() { //partitions are left and right of each other. 0 is on the left, 1 is on the right
            splitSide = Vector2Int.right;
            int splitLocation = Globals.rand.Next(start.x + bspSettings.minCellWidth, end.x - bspSettings.minCellWidth + 1);
            partitions = new Partition[] {
                new Partition(start, new Vector2Int(splitLocation, end.y), depth + 1, bspSettings, roomSettings, connectionSettings),
                new Partition(new Vector2Int(splitLocation, start.y), end, depth + 1, bspSettings, roomSettings, connectionSettings)
            };
            partitions[0].Split();
            partitions[1].Split();
        }
        private void SplitY() { //partitions are on top of each other. 0 is at the bottom, 1 is at the top
            splitSide = Vector2Int.up;
            int splitLocation = Globals.rand.Next(start.y + bspSettings.minCellHeight, end.y - bspSettings.minCellHeight + 1);
            partitions = new Partition[] {
                new Partition(start, new Vector2Int(end.x, splitLocation), depth + 1, bspSettings, roomSettings, connectionSettings),
                new Partition(new Vector2Int(start.x, splitLocation), end, depth + 1, bspSettings, roomSettings, connectionSettings)
            };
            partitions[0].Split();
            partitions[1].Split();
        }

        public void GenerateRoom() {
            if (!isFinal) throw new InvalidOperationException("partition must be a final partition to generate a room");
            room = new List<Shape>();
            int numOfShapes = Globals.rand.Next(roomSettings.numberOfShapes.min, roomSettings.numberOfShapes.max + 1);
            for (int i = 0; i < numOfShapes; i++) {
                int width, height;
                if (roomSettings.rectangleSettings.usePartitionAsMax) {
                    width = Globals.rand.Next(roomSettings.rectangleSettings.width.min, Width - roomSettings.rectangleSettings.border);
                    height = Globals.rand.Next(roomSettings.rectangleSettings.height.min, Height - roomSettings.rectangleSettings.border);
                } else {
                    width = Globals.rand.Next(roomSettings.rectangleSettings.width.min, roomSettings.rectangleSettings.width.max - roomSettings.rectangleSettings.border);
                    height = Globals.rand.Next(roomSettings.rectangleSettings.height.min, roomSettings.rectangleSettings.width.max - roomSettings.rectangleSettings.border);
                }
                int x = Globals.rand.Next(start.x + roomSettings.rectangleSettings.border, end.x - width - roomSettings.rectangleSettings.border + 1);
                int y = Globals.rand.Next(start.y + roomSettings.rectangleSettings.border, end.y - height - roomSettings.rectangleSettings.border + 1);
                Rectangle rectangle = new Rectangle(
                    new Vector2Int(x, y),
                    new Vector2Int(x + width, y + height)
                );
                room.Add(rectangle);
            }
        }

        public void GenerateConnections() {
            if (isFinal) return;
            foreach (Partition partition in partitions) {
                partition.GenerateConnections();
            }
            GenerateConnectionShape();
        }

        internal void GenerateConnectionShape() {
            IntRange range0, range1;
            int min, max, width;
            try {
                if (isFinal) throw new InvalidOperationException("partition must have child partitions to generate connections");
                range0 = partitions[0].GetRange(splitSide);
                range1 = partitions[1].GetRange(splitSide);
                if (!(range0.min + connectionSettings.width.min > range1.max || range1.min + connectionSettings.width.min > range0.max)) { //if it is possible to create a passage with the minimum width
                    min = range0.min > range1.min ? range0.min : range1.min; //get the higher min of the two ranges
                    max = range0.max < range1.max ? range0.max : range1.max; //get the lower max of the two ranges
                    //int width;
                    if (max - min > connectionSettings.width.max) { //if the passage can be built with the widest settings, use connectionSettings.width.max as max width, else use max width possible
                        width = connectionSettings.width.Random();
                    } else {
                        width = Globals.rand.Next(connectionSettings.width.min, max - min + 1);
                    }
                    int startCoord = Globals.rand.Next(min, max - width + 1);
                    Vector2Int start, end;
                    if (splitSide == Vector2Int.up) {
                        start = new Vector2Int(startCoord, partitions[0].RequiredConnectionDepth(Vector2Int.up, new IntRange(startCoord, startCoord + width)));
                        end = new Vector2Int(startCoord + width, partitions[1].RequiredConnectionDepth(Vector2Int.down, new IntRange(startCoord, startCoord + width)));
                    } else if (splitSide == Vector2Int.right) {
                        start = new Vector2Int(partitions[0].RequiredConnectionDepth(Vector2Int.right, new IntRange(startCoord, startCoord + width)), startCoord);
                        end = new Vector2Int(partitions[1].RequiredConnectionDepth(Vector2Int.left, new IntRange(startCoord, startCoord + width)), startCoord + width);
                    } else {
                        throw new Exception("split axis isn't up or right");
                    }
                    connection.Add(new Rectangle(start, end));
                }
            } catch {
                Debug.Log("failed");
            }

            /*
            else {
                int widthCenter = connectionSettings.width.Random();
                int center = Globals.rand.Next(partitions[0].GetRange((splitAxis - Vector2Int.one) * -1).max, partitions[1].GetRange((splitAxis - Vector2Int.one) * -1).min * -1 - widthCenter + 1);
                int width0 = connectionSettings.width.Random();
                int start0 = Globals.rand.Next(range0.min, range0.max - width0 + 1);
                int width1 = connectionSettings.width.Random();
                int start1 = Globals.rand.Next(range1.min, range1.max - width1 + 1);
                connection.Add(new Rectangle(
                    splitAxis * start0 + partitions[0].end * ((splitAxis - Vector2Int.one) * -1), 
                    splitAxis * (start0 + width0) + splitAxis * width0 + ((splitAxis - Vector2Int.one) * -1) * center));
                connection.Add(new Rectangle(
                    splitAxis * start1 + splitAxis * ((splitAxis - Vector2Int.one) * -1) * (center + widthCenter), 
                    splitAxis * (start1 + width1) + ((splitAxis - Vector2Int.one) * -1) * partitions[1].start));
                connection.Add(new Rectangle(
                    splitAxis * start0 + ((splitAxis - Vector2Int.one) * -1) * center,
                    splitAxis * (start1 + width1) * ((splitAxis - Vector2Int.one) * -1) * (center + widthCenter)));
            }
            */
        }

        //calculate how far a passage connection from side needs to go, using width as two coordinates
        internal int RequiredConnectionDepth(Vector2Int side, IntRange range) { //width.min = the left or bottom side of the passage, width.max = the right or top side of the passage
            Dictionary<int, int> coord = new Dictionary<int, int>();
            if (side == Vector2Int.up || side == Vector2Int.right) {
                foreach (Shape shape in Shapes) {
                    foreach (KeyValuePair<int, int> pair in shape.GetSide(side)) {
                        if (coord.ContainsKey(pair.Key)) {
                            coord[pair.Key] = coord[pair.Key] < pair.Value ? pair.Value : coord[pair.Key];
                        } else {
                            coord.Add(pair.Key, pair.Value);
                        }
                    }
                    //Debug.Log(((Rectangle)shape).start.ToString() + ((Rectangle)shape).end.ToString());
                }
                /*
                foreach (KeyValuePair<int, int> pair in coord) {
                    Debug.Log("<" + pair.Key + ", " + pair.Value + ">");
                }
                */
                //Debug.Log(range);
                int required = coord[range.max];
                for (int i = range.min; i < range.max; i++) {
                    required = required > coord[i] ? coord[i] : required;
                }
                return required;
            } else if (side == Vector2Int.down || side == Vector2Int.left) {
                foreach (Shape shape in Shapes) {
                    foreach (KeyValuePair<int, int> pair in shape.GetSide(side)) {
                        if (coord.ContainsKey(pair.Key)) {
                            coord[pair.Key] = coord[pair.Key] > pair.Value ? pair.Value : coord[pair.Key];
                        } else {
                            coord.Add(pair.Key, pair.Value);
                        }
                    }
                    //Debug.Log(((Rectangle)shape).start.ToString() + ((Rectangle)shape).end.ToString());
                }
                /*
                foreach (KeyValuePair<int, int> pair in coord) {
                    Debug.Log("<" + pair.Key + ", " + pair.Value + ">");
                }
                */
                int required = coord[range.max]; //initialize to max width instead of -1
                for (int i = range.min; i < range.max; i++) { //dont check max width
                    required = required < coord[i] ? coord[i] : required;
                }
                return required;
            } else {
                throw new ArgumentException("Axis must be Vector2Int.right, Vector2Int.left, Vector2Int.up, or Vector2Int.down. Current Axis: " + side);
            }
        }

        //recursively return a list of all partitions
        internal List<Partition> IterateThroughAllPartitions() {
            if (isFinal) return new List<Partition>() { this };
            List<Partition> allPartitions = new List<Partition>();
            allPartitions.Add(this);
            foreach (Partition partitionChild in partitions) {
                allPartitions.AddRange(partitionChild.IterateThroughAllPartitions());
            }
            return allPartitions;
        }

        //recursively return a list of final partitions
        internal List<Partition> IterateThroughFinalPartitions() {
            List<Partition> finalPartitions = new List<Partition>();
            foreach (Partition partitionChild in partitions) {
                if (partitionChild.isFinal) {
                    finalPartitions.Add(partitionChild);
                } else {
                    finalPartitions.AddRange(partitionChild.IterateThroughFinalPartitions());
                }
            }
            return finalPartitions;
        }

        //recursively return a list of partitions one level above the final partitions
        internal List<Partition> IterateThroughFinalParentPartitions() {
            List<Partition> finalParentPartitions = new List<Partition>();
            foreach (Partition partitionChild in partitions) {
                if (partitionChild.isFinal) {
                    finalParentPartitions.Add(this);
                } else {
                    finalParentPartitions.AddRange(partitionChild.IterateThroughFinalParentPartitions());
                }
            }
            return finalParentPartitions;
        }
    }

    public abstract class Shape {
        public abstract IntRange GetRange(Vector2Int side); //the width or height of the shape, depending on side
        public abstract Dictionary<int, int> GetSide(Vector2Int side); //the coordinates of the shape from side
    }

    public class Rectangle : Shape {
        public Vector2Int start { get; set; }
        public Vector2Int end { get; set; }
        public Rectangle(Vector2Int start, Vector2Int end) {
            this.start = start;
            this.end = end;
        }

        //get range of the specified side. Vector2Int.up is top side, Vector2Int.right is right side.
        public override IntRange GetRange(Vector2Int side) {
            if (side == Vector2Int.up || side == Vector2Int.down) {
                return new IntRange(start.x, end.x);
            } else if (side == Vector2Int.right || side == Vector2Int.left) {
                return new IntRange(start.y, end.y);
            } else {
                throw new ArgumentException("Axis must be Vector2Int.right, Vector2Int.left, Vector2Int.up, or Vector2Int.down");
            }
        }
        //get a list of the first encountered coordinates of the specified side. Vector2Int.up is top side, Vector2Int.right is right side.
        public override Dictionary<int, int> GetSide(Vector2Int side) {
            Dictionary<int, int> coord = new Dictionary<int, int>();
            if (side == Vector2Int.up) {
                for (int i = start.x; i <= end.x; i++) {
                    coord.Add(i, end.y);
                }
            } else if (side == Vector2Int.right) {
                for (int i = start.y; i <= end.y; i++) {
                    coord.Add(i, end.x);
                }
            } else if (side == Vector2Int.down) {
                for (int i = start.x; i <= end.x; i++) {
                    coord.Add(i, start.y);
                }
            } else if (side == Vector2Int.left) {
                for (int i = start.y; i <= end.y; i++) {
                    coord.Add(i, start.x);
                }
            } else {
                throw new ArgumentException("Axis must be Vector2Int.right, Vector2Int.left, Vector2Int.up, or Vector2Int.down");
            }
            return coord;
        }
    }

    //int range except includes a list of invalid points
    public class ValidRange : IntRange {
        public List<int> invalid { get; set; }

        public ValidRange(int min, int max) : base(min, max) { }
        public ValidRange(IntRange range) : base(range.min, range.max) { }
        public ValidRange(int min, int max, int[] invalid) : this(min, max) {
            this.invalid = invalid.ToList();
        }
        public ValidRange(IntRange range, int[] invalid) : this(range) {
            this.invalid = invalid.ToList();
        }

        public ValidRange Union(ValidRange validRange, bool ignoreOutOfRange = false) {
            return new ValidRange(base.Union(validRange, ignoreOutOfRange), invalid.Union(validRange.invalid).ToArray());
        }
        public ValidRange Intersection(ValidRange validRange) {
            IntRange newRange = base.Intersection(validRange);
            List<int> newInvalid = (List<int>)invalid.Union(validRange.invalid);
            newInvalid.RemoveAll(v => v <= newRange.min || v >= newRange.max);
            return new ValidRange(newRange, newInvalid.ToArray());
        }
    }
}
