using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    public class TransactionFeeChargedLogEventProcessor : IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITotalTransactionFeesMapProvider _totalTransactionFeesMapProvider;
        private LogEvent _interestedEvent;
        private ILogger<TransactionFeeChargedLogEventProcessor> Logger { get; set; }

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;

                var address =
                    _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);

                _interestedEvent = new TransactionFeeCharged().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public TransactionFeeChargedLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ITotalTransactionFeesMapProvider totalTransactionFeesMapProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _totalTransactionFeesMapProvider = totalTransactionFeesMapProvider;
            Logger = NullLogger<TransactionFeeChargedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap)
        {
            var blockHash = block.GetHash();
            var blockHeight = block.Height;
            var totalTxFeesMap = new TotalTransactionFeesMap
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            };

            foreach (var logEvent in logEventsMap.Values.SelectMany(logEvents => logEvents))
            {
                var eventData = new TransactionFeeCharged();
                eventData.MergeFrom(logEvent);
                if (eventData.Symbol == null || eventData.Amount == 0)
                    continue;

                if (totalTxFeesMap.Value.ContainsKey(eventData.Symbol))
                {
                    totalTxFeesMap.Value[eventData.Symbol] += eventData.Amount;
                }
                else
                {
                    totalTxFeesMap.Value[eventData.Symbol] = eventData.Amount;
                }
            }

            if (totalTxFeesMap.Value.Any())
            {
                await _totalTransactionFeesMapProvider.SetTotalTransactionFeesMapAsync(new BlockIndex
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight
                }, totalTxFeesMap);
            }
        }
    }
}