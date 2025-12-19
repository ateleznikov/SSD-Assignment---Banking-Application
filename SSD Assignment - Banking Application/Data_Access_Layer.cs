using Microsoft.Data.Sqlite;
using SSD_Assignment___Banking_Application;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    public class Data_Access_Layer
    {

        private List<Bank_Account> accounts;
        public static String databaseName = "Banking Database.db";
        private static Data_Access_Layer instance = new Data_Access_Layer();

        private Data_Access_Layer()
        {
            accounts = new List<Bank_Account>();
        }

        public static Data_Access_Layer getInstance()
        {
            return instance;
        }

        private SqliteConnection getDatabaseConnection()
        {

            String databaseConnectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = Data_Access_Layer.databaseName,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            return new SqliteConnection(databaseConnectionString);

        }

        private void initialiseDatabase()
        {
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS Bank_Accounts(    
                        accountNo TEXT PRIMARY KEY,
                        name TEXT NOT NULL,
                        address_line_1 TEXT,
                        address_line_2 TEXT,
                        address_line_3 TEXT,
                        town TEXT NOT NULL,
                        balance REAL NOT NULL,
                        accountType INTEGER NOT NULL,
                        overdraftAmount REAL,
                        interestRate REAL
                    ) WITHOUT ROWID
                ";

                command.ExecuteNonQuery();

            }
        }

        public void loadBankAccounts()
        {
            if (!File.Exists(Data_Access_Layer.databaseName))
                initialiseDatabase();
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM Bank_Accounts";
                    SqliteDataReader dr = command.ExecuteReader();

                    while (dr.Read())
                    {

                        int accountType = dr.GetInt16(7);

                        string name = SecurityHelper.Decrypt(dr.GetString(1));
                        string addr1 = SecurityHelper.Decrypt(dr.GetString(2));
                        string addr2 = SecurityHelper.Decrypt(dr.GetString(3));
                        string addr3 = SecurityHelper.Decrypt(dr.GetString(4));
                        string town = SecurityHelper.Decrypt(dr.GetString(5));

                        if (accountType == Account_Type.Current_Account)
                        {
                            Current_Account ca = new Current_Account();
                            ca.accountNo = dr.GetString(0);
                            ca.name = name;
                            ca.address_line_1 = addr1;
                            ca.address_line_2 = addr2;
                            ca.address_line_3 = addr3;
                            ca.town = town;
                            ca.balance = dr.GetDouble(6);
                            ca.overdraftAmount = dr.GetDouble(8);
                            accounts.Add(ca);
                        }
                        else
                        {
                            Savings_Account sa = new Savings_Account();
                            sa.accountNo = dr.GetString(0);
                            sa.name = dr.GetString(1);
                            sa.address_line_1 = dr.GetString(2);
                            sa.address_line_2 = dr.GetString(3);
                            sa.address_line_3 = dr.GetString(4);
                            sa.town = dr.GetString(5);
                            sa.balance = dr.GetDouble(6);
                            sa.interestRate = dr.GetDouble(9);
                            accounts.Add(sa);
                        }


                    }

                }

            }
        }

        public String addBankAccount(Bank_Account ba)
        {
            if (ba.GetType() == typeof(Current_Account))
                ba = (Current_Account)ba;
            else
                ba = (Savings_Account)ba;

            accounts.Add(ba);

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = @"INSERT INTO Bank_Accounts (accountNo, name, address_line_1,
                address_line_2, address_line_3, town, balance, accountType, overdraftAmount, interestRate)
                VALUES ($accNo, $name, $addr1, $addr2, $addr3, $town, $balance, $type, $overdraft, $interest)";

                command.Parameters.AddWithValue("$accNo", ba.accountNo);
                command.Parameters.AddWithValue("$name", SecurityHelper.Encrypt(ba.name));
                command.Parameters.AddWithValue("$addr1", SecurityHelper.Encrypt(ba.address_line_1));
                command.Parameters.AddWithValue("$addr2", SecurityHelper.Encrypt(ba.address_line_2));
                command.Parameters.AddWithValue("$addr3", SecurityHelper.Encrypt(ba.address_line_3));
                command.Parameters.AddWithValue("$town", SecurityHelper.Encrypt(ba.town));
                command.Parameters.AddWithValue("$balance", ba.balance);

                int typeVal = (ba.GetType() == typeof(Current_Account) ? 1 : 2);
                command.Parameters.AddWithValue("$type", typeVal);

                if (ba.GetType() == typeof(Current_Account))
                {
                    Current_Account ca = (Current_Account)ba;
                    command.Parameters.AddWithValue("$overdraft", ca.overdraftAmount);
                    command.Parameters.AddWithValue("$interest", DBNull.Value);
                }
                else
                {
                    Savings_Account sa = (Savings_Account)ba;
                    command.Parameters.AddWithValue("$overdraft", DBNull.Value);
                    command.Parameters.AddWithValue("$interest", sa.interestRate);
                }

                command.ExecuteNonQuery();
            }
            return ba.accountNo;
        }

        public Bank_Account findBankAccountByAccNo(String accNo)
        {

            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    return ba;
                }

            }

            return null;
        }

        public bool closeBankAccount(String accNo)
        {

            Bank_Account toRemove = null;

            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    toRemove = ba;
                    break;
                }

            }

            if (toRemove == null)
                return false;
            else
            {
                accounts.Remove(toRemove);

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM Bank_Accounts WHERE accountNo = '" + toRemove.accountNo + "'";
                    command.ExecuteNonQuery();

                }

                return true;
            }

        }

        public bool lodge(String accNo, double amountToLodge)
        {

            Bank_Account toLodgeTo = null;

            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    ba.lodge(amountToLodge);
                    toLodgeTo = ba;
                    break;
                }

            }

            if (toLodgeTo == null)
                return false;
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Bank_Accounts SET balance = $bal WHERE accountNo = $acc";
                    command.Parameters.AddWithValue("$bal", toLodgeTo.balance);
                    command.Parameters.AddWithValue("$acc", toLodgeTo.accountNo);

                }

                return true;
            }

        }

        public bool withdraw(String accNo, double amountToWithdraw)
        {

            Bank_Account toWithdrawFrom = null;
            bool result = false;

            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    result = ba.withdraw(amountToWithdraw);
                    toWithdrawFrom = ba;
                    break;
                }

            }

            if (toWithdrawFrom == null || result == false)
                return false;
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Bank_Accounts SET balance = $bal WHERE accountNo = $acc";
                    command.Parameters.AddWithValue("$bal", toWithdrawFrom.balance);
                    command.Parameters.AddWithValue("$acc", toWithdrawFrom.accountNo);

                }

                return true;
            }

        }

    }
}