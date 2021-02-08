﻿namespace GWallet.Backend.UtxoCoin.Lightning

open System.IO
open System
open System.Linq

open NBitcoin
open NBitcoin.BuilderExtensions
open DotNetLightning.Channel
open DotNetLightning.Utils
open DotNetLightning.Crypto
open DotNetLightning.Transactions
open DotNetLightning.Transactions.Transactions
open Newtonsoft.Json

open GWallet.Backend
open GWallet.Backend.UtxoCoin
open GWallet.Backend.FSharpUtil
open GWallet.Backend.FSharpUtil.UwpHacks

module public ForceCloseTransaction =

    let internal CreatePunishmentTx (perCommitmentSecret: PerCommitmentSecret)
                                    (commitments: Commitments)
                                    (localChannelPrivKeys: ChannelPrivKeys)
                                    (network: Network)
                                    (account: NormalUtxoAccount)
                                    (rewardAddressOpt: Option<string>)
                                        : Async<Transaction> =
        async {
            let transactionBuilder =
                RemoteForceClose.createPunishmentTx 
                    perCommitmentSecret 
                    commitments 
                    localChannelPrivKeys 
                    network

            let targetAddress =
                let originAddress = (account :> IAccount).PublicAddress
                BitcoinAddress.Create(originAddress, network)

            let rewardAddressOpt = 
                match rewardAddressOpt with 
                | Some rewardAddress -> 
                    BitcoinAddress.Create(rewardAddress, network) |> Some
                | None -> None

            let reward =
                //TODO: MOVE Hardcoded value
                let toLocal =
                    commitments
                        .RemoteCommitAmount()
                        .ToLocal.ToDecimal(MoneyUnit.Satoshi)

                let toRemote =
                    commitments
                        .RemoteCommitAmount()
                        .ToRemote.ToDecimal(MoneyUnit.Satoshi)

                (toLocal + toRemote) * (decimal 0.001)
                |> Money.Satoshis


            match rewardAddressOpt with 
            | Some rewardAddress ->
                transactionBuilder.Send (rewardAddress, reward) |> ignore
                transactionBuilder.SendAllRemaining targetAddress |> ignore
            | None -> 
                transactionBuilder.SendAll targetAddress |> ignore

            let! btcPerKiloByteForFastTrans =
                let averageFee (feesFromDifferentServers: List<decimal>): decimal =
                    feesFromDifferentServers.Sum()
                    / decimal feesFromDifferentServers.Length

                let estimateFeeJob =
                    ElectrumClient.EstimateFee Account.CONFIRMATION_BLOCK_TARGET

                Server.Query (account :> IAccount).Currency (QuerySettings.FeeEstimation averageFee) estimateFeeJob None

            let fee =
                let feeRate =
                    Money(btcPerKiloByteForFastTrans, MoneyUnit.BTC)
                    |> FeeRate

                transactionBuilder.EstimateFees feeRate

            transactionBuilder.SendFees fee |> ignore

            return transactionBuilder.BuildTransaction true
        }