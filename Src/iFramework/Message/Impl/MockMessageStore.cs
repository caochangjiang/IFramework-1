﻿using IFramework.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message.Impl
{
    public class MockMessageStore : IMessageStore
    {
        public bool HasCommandHandled(string commandId)
        {
            return false;
        }

        public bool HasEventHandled(string eventId, string subscriptionName)
        {
            return false;

        }

        public void SaveCommand(IMessageContext commandContext, params IMessageContext[] messageContexts)
        {
        }



        public void RemoveSentCommand(string commandId)
        {

        }

        public void RemovePublishedEvent(string eventId)
        {

        }



        public void Dispose()
        {

        }


        public void HandleEvent(IMessageContext eventContext, string subscriptionName,
                              IEnumerable<IMessageContext> commandContexts,
                              IEnumerable<IMessageContext> messageContexts)
        {
        }



        public IEnumerable<IMessageContext> GetAllUnSentCommands(Func<string, IMessage, string, string, string, SagaInfo, IMessageContext> wrapMessage)
        {
            return null;
        }

        public IEnumerable<IMessageContext> GetAllUnPublishedEvents(Func<string, IMessage, string, string, string, SagaInfo, IMessageContext> wrapMessage)
        {
            return null;
        }

        public void Rollback()
        {

        }

        public void SaveFailedCommand(IMessageContext commandContext, Exception ex = null, params IMessageContext[] eventContexts)
        {
        }

        public void SaveFailHandledEvent(IMessageContext eventContext, string subscriptionName, Exception e, params IMessageContext[] messageContexts)
        {
        }

        public void SaveEvent(IMessageContext eventContext)
        {

        }


    }
}
