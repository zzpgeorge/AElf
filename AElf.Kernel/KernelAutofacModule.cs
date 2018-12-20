﻿using AElf.Common.Serializers;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using Autofac;

namespace AElf.Kernel
{
    public class KernelAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assembly2 = typeof(BlockHeader).Assembly;
            builder.RegisterAssemblyTypes(assembly2).AsImplementedInterfaces();
            
            builder.RegisterType<ProtobufSerializer>().As<IByteSerializer>().SingleInstance();
            
            
            builder.RegisterType<StateStore>().As<IStateStore>().SingleInstance();
            builder.RegisterType<TransactionStore>().As<ITransactionStore>().SingleInstance();
            builder.RegisterType<MerkleTreeStore>().As<IMerkleTreeStore>().SingleInstance();
            builder.RegisterType<BlockHeaderStore>().As<IBlockHeaderStore>();
            builder.RegisterType<BlockBodyStore>().As<IBlockBodyStore>();


            
            builder.RegisterType<StateManager>().As<IStateManager>();
            builder.RegisterType<TransactionManager>().As<ITransactionManager>();
            builder.RegisterType<BinaryMerkleTreeManager>().As<IBinaryMerkleTreeManager>();
            builder.RegisterType<BlockManager>().As<IBlockManager>();

            
            
            
            builder.RegisterType<ChainManagerBasic>().As<IChainManagerBasic>();
            builder.RegisterType<MinersManager>().As<IMinersManager>();
            builder.RegisterType<SmartContractManager>().As<ISmartContractManager>();
            builder.RegisterType<TransactionReceiptManager>().As<ITransactionReceiptManager>();
            builder.RegisterType<TransactionTraceManager>().As<ITransactionTraceManager>();
            builder.RegisterType<TransactionResultManager>().As<ITransactionResultManager>();
            builder.RegisterType<DataStore>().As<IDataStore>();
        }
    }
}