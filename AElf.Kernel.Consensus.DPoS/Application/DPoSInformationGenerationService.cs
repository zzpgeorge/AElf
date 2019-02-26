﻿using System;
using System.Linq;
using AElf.Common;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Infrastructure;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class DPoSInformationGenerationService : IConsensusInformationGenerationService
    {
        private readonly DPoSOptions _dpoSOptions;
        private readonly IAccountService _accountService;
        private readonly ConsensusControlInformation _controlInformation;
        private Hash _inValue;

        public DPoSHint Hint => DPoSHint.Parser.ParseFrom(_controlInformation.ConsensusCommand.Hint);

        public ILogger<DPoSInformationGenerationService> Logger { get; set; }

        public DPoSInformationGenerationService(IOptions<DPoSOptions> consensusOptions, IAccountService accountService,
            ConsensusControlInformation controlInformation)
        {
            _dpoSOptions = consensusOptions.Value;
            _accountService = accountService;
            _controlInformation = controlInformation;

            Logger = NullLogger<DPoSInformationGenerationService>.Instance;
        }

        public byte[] GetFirstExtraInformation()
        {
            return new DPoSExtraInformation
            {
                IsBootMiner = _dpoSOptions.IsBootMiner,
                PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            }.ToByteArray();
        }

        public byte[] GenerateExtraInformation()
        {
            switch (Hint.Behaviour)
            {
                case DPoSBehaviour.InitialTerm:
                    return new DPoSExtraInformation
                    {
                        InitialMiners = {_dpoSOptions.InitialMiners},
                        MiningInterval = DPoSConsensusConsts.MiningInterval,
                        PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                        IsBootMiner = _dpoSOptions.IsBootMiner
                    }.ToByteArray();

                case DPoSBehaviour.PackageOutValue:
                    if (_inValue == null)
                    {
                        // For Round 1.
                        _inValue = Hash.Generate();
                        return new DPoSExtraInformation
                        {
                            HashValue = Hash.FromMessage(_inValue),
                            InValue = Hash.Zero,
                            PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex()
                        }.ToByteArray();
                    }
                    else
                    {
                        var previousInValue = _inValue;
                        var outValue = Hash.FromMessage(_inValue);
                        _inValue = Hash.Generate();
                        return new DPoSExtraInformation
                        {
                            HashValue = outValue,
                            InValue = previousInValue,
                            PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex()
                        }.ToByteArray();
                    }

                case DPoSBehaviour.NextRound:
                    return new DPoSExtraInformation
                    {
                        Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                        PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex()
                    }.ToByteArray();

                case DPoSBehaviour.NextTerm:
                    return new DPoSExtraInformation
                    {
                        Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                        ChangeTerm = true,
                        PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex()
                    }.ToByteArray();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public byte[] GenerateExtraInformationForTransaction(byte[] consensusInformation, int chainId)
        {
            var information = DPoSInformation.Parser.ParseFrom(consensusInformation);

            Logger.LogInformation($"Current behaviour: {Hint.Behaviour.ToString()}.");
            switch (Hint.Behaviour)
            {
                case DPoSBehaviour.InitialTerm:
                    information.NewTerm.ChainId = chainId;
                    information.NewTerm.FirstRound.MiningInterval = _dpoSOptions.MiningInterval;
                    return new DPoSExtraInformation
                    {
                        NewTerm = information.NewTerm
                    }.ToByteArray();

                case DPoSBehaviour.PackageOutValue:
                    var currentMinerInformation = information.CurrentRound.RealTimeMinersInfo
                        .OrderByDescending(m => m.Value.Order).First(m => m.Value.OutValue != null).Value;
                    Logger.LogInformation($"Round number: {information.CurrentRound.RoundNumber}\nOrder: {currentMinerInformation.Order}");
                    return new DPoSExtraInformation
                    {
                        ToPackage = new ToPackage
                        {
                            OutValue = currentMinerInformation.OutValue,
                            RoundId = information.CurrentRound.RoundId,
                            Signature = currentMinerInformation.Signature,
                            PromiseTinyBlocks = currentMinerInformation.PromisedTinyBlocks
                        },
                        ToBroadcast = new ToBroadcast
                        {
                            InValue = _inValue,
                            RoundId = information.CurrentRound.RoundId
                        }
                    }.ToByteArray();

                case DPoSBehaviour.NextRound:
                    return new DPoSExtraInformation
                    {
                        Forwarding = information.Forwarding,
                        Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                    }.ToByteArray();

                case DPoSBehaviour.NextTerm:
                    return new DPoSExtraInformation
                    {
                        Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                        ChangeTerm = true,
                        NewTerm = information.NewTerm
                    }.ToByteArray();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}