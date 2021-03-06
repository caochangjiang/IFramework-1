﻿using IFramework.Config;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using IFramework.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.EntityFramework
{
    public class AppUnitOfWork : UnitOfWork, IAppUnitOfWork
    {
        protected IMessagePublisher _messagePublisher;
        protected IMessageStore _messageStore;
        protected IMessageQueueClient _messageQueueClient;
        protected List<MessageState> _eventMessageStates;

        public AppUnitOfWork(IEventBus eventBus,
                             ILoggerFactory loggerFactory,
                             IMessagePublisher eventPublisher,
                             IMessageQueueClient messageQueueClient,
                             IMessageStore messageStore)
            : base(eventBus, loggerFactory)
        {
            _dbContexts = new List<MSDbContext>();
            _eventBus = eventBus;
            _messageStore = messageStore;
            _messagePublisher = eventPublisher;
            _messageQueueClient = messageQueueClient;
            _eventMessageStates = new List<MessageState>();
        }

        protected override void BeforeCommit()
        {
            base.BeforeCommit();
            _eventMessageStates.Clear();
            _eventBus.GetEvents().ForEach(@event =>
            {
                var topic = @event.GetTopic();
                if (!string.IsNullOrEmpty(topic))
                {
                    topic = Configuration.Instance.FormatAppName(topic);
                }
                var eventContext = _messageQueueClient.WrapMessage(@event, null, topic, @event.Key);
                _eventMessageStates.Add(new MessageState(eventContext));
            });

            _eventBus.GetToPublishAnywayMessages().ForEach(@event =>
            {
                var topic = @event.GetTopic();
                if (!string.IsNullOrEmpty(topic))
                {
                    topic = Configuration.Instance.FormatAppName(topic);
                }
                var eventContext = _messageQueueClient.WrapMessage(@event, null, topic, @event.Key);
                _eventMessageStates.Add(new MessageState(eventContext));
            });
            _messageStore.SaveCommand(null, _eventMessageStates.Select(s => s.MessageContext).ToArray());
        }

        protected override void AfterCommit()
        {
            base.AfterCommit();
            _eventBus.ClearMessages();
            try
            {
                if (_messagePublisher != null && _eventMessageStates.Count > 0)
                {
                    _messagePublisher.SendAsync(_eventMessageStates.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"_messagePublisher SendAsync error", ex);
            }
        }
    }
}
