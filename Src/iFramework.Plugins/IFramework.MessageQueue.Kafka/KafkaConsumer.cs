﻿using Kafka.Client.Cfg;
using Kafka.Client.Consumers;
using Kafka.Client.Helper;
using Kafka.Client.Requests;
using System.Collections.Generic;
using IFramework.Infrastructure;
using System.Linq;
using KafkaMessages = Kafka.Client.Messages;
using Kafka.Client.Serialization;
using System.Threading;
using IFramework.Message;
using System.Collections.Concurrent;
using IFramework.Message.Impl;
using IFramework.Config;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.MessageQueue.MSKafka.MessageFormat;

namespace IFramework.MessageQueue.MSKafka
{
    public class KafkaConsumer
    {
        public string ZkConnectionString { get; protected set; }
        public string Topic { get; protected set; }
        public string GroupId { get; protected set; }
        public string ConsumerId { get; protected set; }
        public ZookeeperConsumerConnector ZkConsumerConnector { get; protected set; }
        public ConsumerConfiguration ConsumerConfiguration { get; protected set; }
        public ConcurrentDictionary<int, SlidingDoor> SlidingDoors { get; protected set; }
        protected int _fullLoadThreshold;
        protected int _waitInterval;
        protected ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(KafkaConsumer).Name);
        public KafkaConsumer(string zkConnectionString, string topic, string groupId, string consumerId = null, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            _fullLoadThreshold = fullLoadThreshold;
            _waitInterval = waitInterval;
            SlidingDoors = new ConcurrentDictionary<int, SlidingDoor>();
            ZkConnectionString = zkConnectionString;
            Topic = topic;
            GroupId = groupId;
            ConsumerId = consumerId ?? this.GetType().Name;
            ConsumerConfiguration = new ConsumerConfiguration
            {
                AutoCommit = false,
                GroupId = GroupId,
                ConsumerId = ConsumerId,
                MaxFetchBufferLength = KafkaSimpleManagerConfiguration.DefaultBufferSize,
                FetchSize = KafkaSimpleManagerConfiguration.DefaultFetchSize,
                AutoOffsetReset = OffsetRequest.LargestTime,
                NumberOfTries = 3,
                ZooKeeper = new ZooKeeperConfiguration(zkConnectionString, 3000, 3000, 1000)
            };
            ZkConsumerConnector = new ZookeeperConsumerConnector(ConsumerConfiguration, true);
        }

        IKafkaMessageStream<KafkaMessages.Message> _stream;
        public IKafkaMessageStream<KafkaMessages.Message> Stream
        {
            get
            {
                if (_stream == null)
                {
                    var topicDic = new Dictionary<string, int>() {
                        {Topic, 1 }
                    };
                    var streams = ZkConsumerConnector.CreateMessageStreams(topicDic, new DefaultDecoder());
                    _stream = streams[Topic][0];
                }
                return _stream;
            }
        }


        public void BlockIfFullLoad()
        {
            while (SlidingDoors.Sum(d => d.Value.MessageCount) > _fullLoadThreshold)
            {
                Thread.Sleep(_waitInterval);
                _logger.Warn($"working is full load sleep 1000 ms");
            }
        }

        internal void AddMessage(KafkaMessages.Message message)
        {
            var slidingDoor = SlidingDoors.GetOrAdd(message.PartitionId.Value, partition =>
            {
                return new SlidingDoor(CommitOffset, 
                                       partition,
                                       Configuration.Instance.GetCommitPerMessage());
            });
            slidingDoor.AddOffset(message.Offset);
        }

        internal void RemoveMessage(int partition, long offset)
        {
            var slidingDoor = SlidingDoors.TryGetValue(partition);
            if (slidingDoor == null)
            {
                throw new System.Exception("partition slidingDoor not exists");
            }
            slidingDoor.RemoveOffset(offset);
        }

        internal IEnumerable<KafkaMessages.Message> GetMessages(CancellationToken cancellationToken)
        {
            return Stream.GetCancellable(cancellationToken);
        }

        internal void CommitOffset(IMessageContext messageContext)
        {
            var message = (messageContext as MessageContext);
            RemoveMessage(message.Partition, message.Offset);
        }

        internal void CommitOffset(int partition, long offset)
        {
            ZkConsumerConnector.CommitOffset(Topic, partition, offset, false);
        }

        internal void Stop()
        {
            if (ZkConsumerConnector != null)
            {
                ZkConsumerConnector.Dispose();
                ZkConsumerConnector = null;
            }
        }
    }
}