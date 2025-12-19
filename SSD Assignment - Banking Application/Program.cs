using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Banking_Application
{
    public class Program
    {
        public static void LogTransaction(string who, string accNo, string type, string details)
        {
            string source = "SSD Banking Application";
            string log = "Application";

            try
            {
                if (!EventLog.SourceExists(source))
                    EventLog.CreateEventSource(source, log);

                string message = $"WHO: {who} | Account: {accNo} | WHAT: {type} | DETAILS: {details} | WHEN: {DateTime.Now}";
                EventLog.WriteEntry(source, message, EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: Could not write to Event Log: " + ex.Message);
            }
        }

        public static bool IsInputValid(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            Regex r = new Regex("^[a-zA-Z0-9\\s,.-]+$");
            return r.IsMatch(input);
        }

        public static bool Login()
        {
            Console.WriteLine("--- SECURE BANKING SYSTEM LOGIN ---");

            try
            {
                Console.Write("Username: ");
                string user = Console.ReadLine();

                Console.Write("Password: ");
                string pass = "";
                do
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                    {
                        pass += key.KeyChar;
                        Console.Write("*");
                    }
                    else if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass = pass.Substring(0, (pass.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                } while (true);
                Console.WriteLine();

                using (PrincipalContext ctx = new PrincipalContext(ContextType.Domain, "ITSLIGO.LAN"))
                {
                    if (ctx.ValidateCredentials(user, pass))
                    {
                        UserPrincipal up = UserPrincipal.FindByIdentity(ctx, user);
                        if (up != null && up.IsMemberOf(ctx, IdentityType.Name, "Bank Teller"))
                        {
                            LogTransaction(user, "N/A", "Login", "Success");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("\nError: Access Denied. Not a Bank Teller.");
                            LogTransaction(user, "N/A", "Login Failed", "Not in Group");
                        }
                    }
                    else
                    {
                        Console.WriteLine("\nError: Invalid Username or Password.");
                        LogTransaction(user, "N/A", "Login Failed", "Bad Credentials");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nSystem Error (AD): " + ex.Message);
            }
            return false;
        }

        public static bool IsAdminUser()
        {
            try
            {
                using (PrincipalContext ctx = new PrincipalContext(ContextType.Domain, "ITSLIGO.LAN"))
                {
                    Console.Write("Enter Admin Username: ");
                    string user = Console.ReadLine();
                    Console.Write("Enter Admin Password: ");

                    string pass = "";
                    do
                    {
                        ConsoleKeyInfo key = Console.ReadKey(true);
                        if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                        {
                            pass += key.KeyChar;
                            Console.Write("*");
                        }
                        else if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                        {
                            pass = pass.Substring(0, (pass.Length - 1));
                            Console.Write("\b \b");
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            break;
                        }
                    } while (true);
                    Console.WriteLine();

                    if (ctx.ValidateCredentials(user, pass))
                    {
                        UserPrincipal up = UserPrincipal.FindByIdentity(ctx, user);
                        if (up != null && up.IsMemberOf(ctx, IdentityType.Name, "Bank Teller Administrator"))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception) { return false; }
            return false;
        }

        public static void Main(string[] args)
        {
            if (!Login())
            {
                Console.WriteLine("\nACCESS DENIED. Application Closing.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\nAccess Granted. Loading Database...");
            Thread.Sleep(1000);
            Console.Clear();

            Data_Access_Layer dal = Data_Access_Layer.getInstance();
            dal.loadBankAccounts();
            bool running = true;

            do
            {
                Console.WriteLine("");
                Console.WriteLine("***Banking Application Menu***");
                Console.WriteLine("1. Add Bank Account");
                Console.WriteLine("2. Close Bank Account");
                Console.WriteLine("3. View Account Information");
                Console.WriteLine("4. Make Lodgement");
                Console.WriteLine("5. Make Withdrawal");
                Console.WriteLine("6. Exit");
                Console.WriteLine("CHOOSE OPTION:");
                String option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        String accountType = "";
                        int loopCount = 0;

                        do
                        {
                            if (loopCount > 0) Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");
                            Console.WriteLine("\n***Account Types***:");
                            Console.WriteLine("1. Current Account.");
                            Console.WriteLine("2. Savings Account.");
                            Console.WriteLine("CHOOSE OPTION:");
                            accountType = Console.ReadLine();
                            loopCount++;
                        } while (!(accountType.Equals("1") || accountType.Equals("2")));

                        String name = "";
                        loopCount = 0;
                        do
                        {
                            if (loopCount > 0) Console.WriteLine("INVALID NAME ENTERED - PLEASE TRY AGAIN");
                            Console.WriteLine("Enter Name: ");
                            name = Console.ReadLine();

                            if (!IsInputValid(name)) { name = ""; loopCount++; }

                        } while (name.Equals(""));

                        String addressLine1 = "";
                        loopCount = 0;
                        do
                        {
                            if (loopCount > 0) Console.WriteLine("INVALID ADDRESS ENTERED");
                            Console.WriteLine("Enter Address Line 1: ");
                            addressLine1 = Console.ReadLine();
                            if (!IsInputValid(addressLine1)) { addressLine1 = ""; loopCount++; }
                        } while (addressLine1.Equals(""));

                        Console.WriteLine("Enter Address Line 2: ");
                        String addressLine2 = Console.ReadLine();
                        if (!string.IsNullOrEmpty(addressLine2) && !IsInputValid(addressLine2)) addressLine2 = "Invalid Input Replaced";

                        Console.WriteLine("Enter Address Line 3: ");
                        String addressLine3 = Console.ReadLine();

                        String town = "";
                        loopCount = 0;
                        do
                        {
                            if (loopCount > 0) Console.WriteLine("INVALID TOWN ENTERED");
                            Console.WriteLine("Enter Town: ");
                            town = Console.ReadLine();
                            if (!IsInputValid(town)) { town = ""; loopCount++; }
                        } while (town.Equals(""));

                        double balance = -1;
                        loopCount = 0;
                        do
                        {
                            if (loopCount > 0) Console.WriteLine("INVALID BALANCE");
                            Console.WriteLine("Enter Opening Balance: ");
                            String balanceString = Console.ReadLine();
                            try { balance = Convert.ToDouble(balanceString); }
                            catch { loopCount++; }
                        } while (balance < 0);

                        Bank_Account ba;

                        if (Convert.ToInt32(accountType) == Account_Type.Current_Account)
                        {
                            double overdraftAmount = -1;
                            loopCount = 0;
                            do
                            {
                                if (loopCount > 0) Console.WriteLine("INVALID OVERDRAFT");
                                Console.WriteLine("Enter Overdraft Amount: ");
                                String overdraftAmountString = Console.ReadLine();
                                try { overdraftAmount = Convert.ToDouble(overdraftAmountString); }
                                catch { loopCount++; }
                            } while (overdraftAmount < 0);

                            ba = new Current_Account(name, addressLine1, addressLine2, addressLine3, town, balance, overdraftAmount);
                        }
                        else
                        {
                            double interestRate = -1;
                            loopCount = 0;
                            do
                            {
                                if (loopCount > 0) Console.WriteLine("INVALID INTEREST RATE");
                                Console.WriteLine("Enter Interest Rate: ");
                                String interestRateString = Console.ReadLine();
                                try { interestRate = Convert.ToDouble(interestRateString); }
                                catch { loopCount++; }
                            } while (interestRate < 0);

                            ba = new Savings_Account(name, addressLine1, addressLine2, addressLine3, town, balance, interestRate);
                        }

                        String accNo = dal.addBankAccount(ba);
                        Console.WriteLine("New Account Number Is: " + accNo);
                        LogTransaction("CurrentTeller", accNo, "Account Creation", "New Account Added");
                        break;

                    case "2":
                        Console.WriteLine("Enter Account Number to Close: ");
                        accNo = Console.ReadLine();
                        ba = dal.findBankAccountByAccNo(accNo);

                        if (ba is null)
                        {
                            Console.WriteLine("Account Does Not Exist");
                        }
                        else
                        {
                            Console.WriteLine(ba.ToString());
                            Console.WriteLine("WARNING: Closing an account requires Administrator Approval.");

                            if (IsAdminUser())
                            {
                                Console.WriteLine("Admin Verified. Proceed with Deletion (Y/N)?");
                                String ans = Console.ReadLine();
                                if (ans.Equals("Y", StringComparison.OrdinalIgnoreCase))
                                {
                                    dal.closeBankAccount(accNo);
                                    LogTransaction("Admin", accNo, "Account Closure", "Approved by Admin");
                                    Console.WriteLine("Account Closed Successfully.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("ACCESS DENIED: You are not a Bank Teller Administrator.");
                                LogTransaction("CurrentTeller", accNo, "Closure Attempt Failed", "Insufficient Privileges");
                            }
                        }
                        break;

                    case "3":
                        Console.WriteLine("Enter Account Number: ");
                        accNo = Console.ReadLine();
                        ba = dal.findBankAccountByAccNo(accNo);
                        if (ba is null) Console.WriteLine("Account Does Not Exist");
                        else
                        {
                            Console.WriteLine(ba.ToString());
                            LogTransaction("CurrentTeller", accNo, "View Account", "Viewed Details");
                        }
                        break;

                    case "4":
                        Console.WriteLine("Enter Account Number: ");
                        accNo = Console.ReadLine();
                        ba = dal.findBankAccountByAccNo(accNo);
                        if (ba is null) Console.WriteLine("Account Does Not Exist");
                        else
                        {
                            double amountToLodge = -1;
                            loopCount = 0;
                            do
                            {
                                if (loopCount > 0) Console.WriteLine("INVALID AMOUNT");
                                Console.WriteLine("Enter Amount To Lodge: ");
                                String amountToLodgeString = Console.ReadLine();
                                try { amountToLodge = Convert.ToDouble(amountToLodgeString); }
                                catch { loopCount++; }
                            } while (amountToLodge < 0);

                            dal.lodge(accNo, amountToLodge);
                            LogTransaction("CurrentTeller", accNo, "Lodgement", $"Amount: {amountToLodge}");
                        }
                        break;

                    case "5":
                        Console.WriteLine("Enter Account Number: ");
                        accNo = Console.ReadLine();
                        ba = dal.findBankAccountByAccNo(accNo);
                        if (ba is null) Console.WriteLine("Account Does Not Exist");
                        else
                        {
                            double amountToWithdraw = -1;
                            loopCount = 0;
                            do
                            {
                                if (loopCount > 0) Console.WriteLine("INVALID AMOUNT");
                                Console.WriteLine("Enter Amount To Withdraw (€" + ba.getAvailableFunds() + " Available): ");
                                String amountToWithdrawString = Console.ReadLine();
                                try { amountToWithdraw = Convert.ToDouble(amountToWithdrawString); }
                                catch { loopCount++; }
                            } while (amountToWithdraw < 0);

                            bool withdrawalOK = dal.withdraw(accNo, amountToWithdraw);
                            if (withdrawalOK == false) Console.WriteLine("Insufficient Funds Available.");
                            else LogTransaction("CurrentTeller", accNo, "Withdrawal", $"Amount: {amountToWithdraw}");
                        }
                        break;

                    case "6":
                        running = false;
                        dal = null;
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        Console.WriteLine("System Shutdown Complete.");
                        break;

                    default:
                        Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");
                        break;
                }

            } while (running != false);
        }
    }
}