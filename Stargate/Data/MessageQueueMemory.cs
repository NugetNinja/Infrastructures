﻿using Aiursoft.SDK.Models.Stargate;
using Aiursoft.XelNaga.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Aiursoft.Stargate.Data
{
    public class StargateMemory : ISingletonDependency
    {
        private Dictionary<int, int> ConnectedCount { get; set; } = new Dictionary<int, int>();
        public List<Message> Messages { get; set; } = new List<Message>();

        public void AddConnectedCount(int channelId)
        {
            if (ConnectedCount.ContainsKey(channelId))
            {
                ConnectedCount[channelId]++;
            }
            else
            {
                ConnectedCount[channelId] = 1;
            }
        }

        public int GetConnectedCount(int channelId)
        {
            if (ConnectedCount.ContainsKey(channelId))
            {
                return ConnectedCount[channelId];
            }
            return 0;
        }

        public int GetAllConnectedCount()
        {
            return ConnectedCount.Sum(t => t.Value);
        }

        public void ReduceConnectedCount(int channelId)
        {
            if (ConnectedCount.ContainsKey(channelId))
            {
                ConnectedCount[channelId]--;
            }
            else
            {
                // Shouldn't happen. But don't throw exception.
                ConnectedCount[channelId] = 0;
            }
        }
    }
}
