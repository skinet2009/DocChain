﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using EthereumLibrary.ContractService;
using EthereumLibrary.Helper;
using EthereumLibrary.Model;
using Nethereum.Geth;
using Nethereum.Hex.HexTypes;

namespace EthereumLibrary.Service
{
    public class EthereumFileService : IEthereumFileService
    {
        private UsersAndFilesService _contractService;
        private string _walletAddress;
        private HexBigInteger _gas;


        public EthereumFileService(Web3Geth web3, string contractAddress, string walletAddress, BigInteger gas)
        {
            _contractService = new UsersAndFilesService(web3, contractAddress);
            _walletAddress = walletAddress;
            _gas = new HexBigInteger(gas);
        }

        public async Task<IEnumerable<IEthereumFile>> GetAsyncCall(string ownerLogin, string ownerPassword,
            IEnumerable<BigInteger> ids)
        {
            var userFiles = new List<IEthereumFile>();
            foreach (var id in ids)
            {
                userFiles.Add(await GetAsyncCall(ownerLogin, ownerPassword, id));
            }

            return userFiles;
        }

        public async Task<IEnumerable<IEthereumFile>> GetAsyncCall(string ownerLogin, string ownerPassword)
        {
            var login = CastHelper.StringToBytes32(ownerLogin);
            var password = CastHelper.StringToBytes32(ownerPassword);

            var ids = await _contractService.GetFileIdsAsyncCall(login, password);

            return await GetAsyncCall(ownerLogin, ownerPassword, ids.ToArray());
        }

        public async Task<IEthereumFile> GetAsyncCall(string ownerLogin, string ownerPassword, BigInteger id)
        {
            var login = CastHelper.StringToBytes32(ownerLogin);
            var password = CastHelper.StringToBytes32(ownerPassword);

            var part1 = await _contractService.GetFilePart1AsyncCall(login, password, id);
            var part2 = await _contractService.GetFilePart2AsyncCall(login, password, id);

            return new ReadableIpfsFileDto(id, part1, part2);
        }

        public async Task<IEthereumFile> AddAsync(
            string login, string password,
            string type, string hash, long size, string name, string description, DateTime created)
        {
            var param = new
            {
                Login = CastHelper.StringToBytes32(login),
                Password = CastHelper.StringToBytes32(password),
                Mime = CastHelper.StringToBytes32(type),
                Hash = CastHelper.ToDescriptionType(hash),
                Size = CastHelper.StringToBytes32(size.ToString()),
                Name = CastHelper.ToFileNameType(name),
                Description = CastHelper.ToDescriptionType(description),
                Timestamp = (int) ((DateTimeOffset) created).ToUnixTimeSeconds()
            };

            // send call to get output value 
            var response = await _contractService.AddFileAsyncCall(
                param.Login, param.Password,
                param.Mime, param.Hash, param.Size, param.Name, param.Description, param.Timestamp);

            // send transaction & wait it to be mined
            var transactionHash = await _contractService.AddFileAsync(
                _walletAddress,
                param.Login, param.Password,
                param.Mime, param.Hash, param.Size, param.Name, param.Description, param.Timestamp,
                _gas);

            var receipt = await _contractService.MineAndGetReceiptAsync(transactionHash);
            
            return await GetAsyncCall(login, password, response.Fileindex);
        }

        public async Task<IEnumerable<BigInteger>> GetIdsAsyncCall(string ownerLogin, string ownerPassword)
        {
            var login = CastHelper.StringToBytes32(ownerLogin);
            var password = CastHelper.StringToBytes32(ownerPassword);

            var ids = await _contractService.GetFileIdsAsyncCall(login, password);
            
            return ids.ToArray();
        }

        public async Task<bool> SetNameAsync(
            string login, string password, BigInteger id, string newName, DateTime modified)
        {
            var param = new
            {
                Login = CastHelper.StringToBytes32(login),
                Password = CastHelper.StringToBytes32(password),
                Name = CastHelper.ToFileNameType(newName),
                Timestamp = (int) ((DateTimeOffset) modified).ToUnixTimeSeconds()
            };

            var transactionHash = await _contractService.SetFileNameAsync(
                _walletAddress, param.Login, param.Password, id, param.Name, param.Timestamp, _gas);

            var receipt = await _contractService.MineAndGetReceiptAsync(transactionHash);

            return true;
        }

        public async Task<bool> SetDescriptionAsync(
            string login, string password, BigInteger id, string newDescription, DateTime modified)
        {
            var param = new
            {
                Login = CastHelper.StringToBytes32(login),
                Password = CastHelper.StringToBytes32(password),
                Description = CastHelper.ToDescriptionType(newDescription),
                Timestamp = (int) ((DateTimeOffset) modified).ToUnixTimeSeconds()
            };

            var transactionHash = await _contractService.SetFileDescriptionAsync(
                _walletAddress, param.Login, param.Password, id, param.Description, param.Timestamp, _gas);

            var receipt = await _contractService.MineAndGetReceiptAsync(transactionHash);

            return true;
        }

        public async Task<bool> DeleteAsync(string login, string password, BigInteger id)
        {
            var transactionHash = await _contractService.DeleteFileAsync(
                _walletAddress, CastHelper.StringToBytes32(login), CastHelper.StringToBytes32(password), id, _gas);

            var receipt = await _contractService.MineAndGetReceiptAsync(transactionHash);

            return true;
        }
    }
}