using SimpleSummon.Domain;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    internal sealed class NetworkSignDrawingState
    {
        private readonly SignDrawingModel model;
        private readonly NetworkList<NetworkSignPoint> points;

        public NetworkSignDrawingState(
            SignDrawingModel model,
            NetworkList<NetworkSignPoint> points)
        {
            this.model = model;
            this.points = points;
        }

        public int Count(bool isSpawned) => isSpawned ? points.Count : model.Points.Count;

        public NetworkSignPoint Get(bool isSpawned, int index) => isSpawned
            ? points[index]
            : NetworkSignPointMapper.ToNetwork(model.Points[index]);

        public void PublishAppended(int previousCount)
        {
            for (int i = previousCount; i < model.Points.Count; i++)
            {
                points.Add(NetworkSignPointMapper.ToNetwork(model.Points[i]));
            }
        }

        public void PublishAll()
        {
            points.Clear();
            foreach (SignStrokePoint point in model.Points)
            {
                points.Add(NetworkSignPointMapper.ToNetwork(point));
            }
        }
    }
}
