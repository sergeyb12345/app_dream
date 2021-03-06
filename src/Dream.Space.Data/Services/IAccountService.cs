﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dream.Space.Data.Requests.Accounts;
using Dream.Space.Models.Accounts;

namespace Dream.Space.Data.Services
{
    public interface IAccountService
    {
        Task Deposit(DepositRequest request);
        Task Withdraw(WithdrawRequest request);
        Task<decimal> GetMaxRiskValue(int accountId,  DateTime date);
        Task<AccountTradeModel> CreateTrade(CreateTradeRequest request);
        Task<AccountTradeModel> CloseTrade(CloseTradeRequest request);
        Task<AccountTradeModel> CloseTradePartially(CloseTradePartiallyRequest request);
        Task<decimal> GetOverallBalance(int accountId, DateTime date);
        Task<decimal> GetBalanceFromTrades(int accountId, DateTime date);
        Task<List<AccountModel>> GetAccounts(string userId);

        Task<AccountModel> CreateAccount(CreateAccountRequest request);
    }
}
