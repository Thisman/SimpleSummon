using SimpleSummon.Domain;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    internal sealed class NetworkSummonDrawingState
    {
        private readonly SummonRitualModel model;
        private readonly NetworkList<NetworkSummonPoint> points;

        public NetworkSummonDrawingState(
            SummonRitualModel model,
            NetworkList<NetworkSummonPoint> points)
        {
            this.model = model;
            this.points = points;
        }

        public int Count(bool isSpawned) => isSpawned ? points.Count : model.Points.Count;

        public NetworkSummonPoint Get(bool isSpawned, int index) => isSpawned
            ? points[index]
            : NetworkSummonPointMapper.ToNetwork(model.Points[index]);

        public void PublishAppended(int previousCount)
        {
            for (int i = previousCount; i < model.Points.Count; i++)
            {
                points.Add(NetworkSummonPointMapper.ToNetwork(model.Points[i]));
            }
        }

        public void PublishAll()
        {
            points.Clear();
            foreach (SummonStrokePoint point in model.Points)
            {
                points.Add(NetworkSummonPointMapper.ToNetwork(point));
            }
        }
    }
}
