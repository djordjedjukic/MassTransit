﻿namespace MassTransit.Azure.Table.Contexts
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using GreenPipes;
    using MassTransit.Saga;
    using Saga;
    using Util;


    public class AzureTableSagaRepositoryContext<TSaga, TMessage> :
        ConsumeContextScope<TMessage>,
        SagaRepositoryContext<TSaga, TMessage>
        where TSaga : class, IVersionedSaga
        where TMessage : class
    {
        readonly ConsumeContext<TMessage> _consumeContext;
        readonly DatabaseContext<TSaga> _context;
        readonly ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> _factory;

        public AzureTableSagaRepositoryContext(DatabaseContext<TSaga> context, ConsumeContext<TMessage> consumeContext,
            ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory)
            : base(consumeContext)
        {
            _context = context;
            _consumeContext = consumeContext;
            _factory = factory;
        }

        public Task<SagaConsumeContext<TSaga, TMessage>> Add(TSaga instance)
        {
            return _factory.CreateSagaConsumeContext(_context, _consumeContext, instance, SagaConsumeContextMode.Add);
        }

        public async Task<SagaConsumeContext<TSaga, TMessage>> Insert(TSaga instance)
        {
            try
            {
                await _context.Insert(instance, CancellationToken).ConfigureAwait(false);

                _consumeContext.LogInsert<TSaga, TMessage>(instance.CorrelationId);

                return await _factory.CreateSagaConsumeContext(_context, _consumeContext, instance, SagaConsumeContextMode.Insert).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _consumeContext.LogInsertFault<TSaga, TMessage>(ex, instance.CorrelationId);

                return default;
            }
        }

        public async Task<SagaConsumeContext<TSaga, TMessage>> Load(Guid correlationId)
        {
            var instance = await _context.Load(correlationId, CancellationToken).ConfigureAwait(false);
            if (instance == null)
                return default;

            return await _factory.CreateSagaConsumeContext(_context, _consumeContext, instance, SagaConsumeContextMode.Load).ConfigureAwait(false);
        }

        public Task Save(SagaConsumeContext<TSaga> context)
        {
            return _context.Add(context);
        }

        public Task Update(SagaConsumeContext<TSaga> context)
        {
            return _context.Update(context);
        }

        public Task Delete(SagaConsumeContext<TSaga> context)
        {
            return _context.Delete(context);
        }

        public Task Discard(SagaConsumeContext<TSaga> context)
        {
            return TaskUtil.Completed;
        }

        public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContext<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
            where T : class
        {
            return _factory.CreateSagaConsumeContext(_context, consumeContext, instance, mode);
        }
    }


    public class CosmosTableSagaRepositoryContext<TSaga> :
        BasePipeContext,
        SagaRepositoryContext<TSaga>
        where TSaga : class, IVersionedSaga
    {
        readonly DatabaseContext<TSaga> _context;

        public CosmosTableSagaRepositoryContext(DatabaseContext<TSaga> context, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            _context = context;
        }

        public Task<SagaRepositoryQueryContext<TSaga>> Query(ISagaQuery<TSaga> query, CancellationToken cancellationToken)
        {
            throw new NotImplementedByDesignException("Azure Table saga repository does not support queries");
        }

        public Task<TSaga> Load(Guid correlationId)
        {
            return _context.Load(correlationId, CancellationToken);
        }
    }
}