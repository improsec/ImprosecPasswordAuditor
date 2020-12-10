using DSInternals.Common.Cryptography;
using DSInternals.Common.Data;
using DSInternals.Replication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImproService
{
    class Scanner
    {
        [DllImport("ipscanner.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void AddSource(string filename);

        [DllImport("ipscanner.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void ClearSources();

        [DllImport("ipscanner.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void TestHashes(byte[] input, int count, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UI1)] out byte[] output);

        private static readonly object Lock = new object();

        public static Dictionary<Guid, uint> Accounts { get; } = new Dictionary<Guid, uint>();

        public static void LoadAccountStates()
        {
            string account_file = Config.ReadFile("data", "account_file");

            if (File.Exists(account_file))
            {
                foreach (var entry in File.ReadAllLines(account_file))
                {
                    int delim = entry.IndexOf(':');

                    if (Guid.TryParse(entry.Substring(0, delim), out Guid g))
                        Accounts[g] = Convert.ToUInt32(entry.Substring(delim + 1), 16);
                }
            }
        }

        public static void SaveAccountStates()
        {
            List<string> list = new List<string>();

            foreach (var user in Accounts)
                list.Add(string.Format("{0}:{1}", user.Key.ToString(), user.Value.ToString("X8")));

            File.WriteAllLines(Config.ReadFile("data", "account_file"), list.ToArray());
        }

        public static List<DSAccount> ReadAllUsers(string domain_server, string naming_context)
        {
            lock (Lock)
            {
                using (var client = new DirectoryReplicationClient(domain_server, RpcProtocol.TCP))
                {
                    return client.GetAccounts(naming_context)
                        .Where(a => a.SamAccountType == SamAccountType.User)
                        .ToList();
                }
            }
        }

        public static List<DSAccount> UpdateAccounts(List<DSAccount> users)
        {
            var updated = new List<DSAccount>();

            foreach (var user in users)
            {
                uint c = user.NTHash == null ? 0 : Crc32.Calculate(user.NTHash);

                if (!Accounts.ContainsKey(user.Guid))
                {
                    Accounts.Add(user.Guid, c);
                    updated.Add(user);
                }
                else if (Accounts[user.Guid] != c)
                {
                    Accounts[user.Guid] = c;
                    updated.Add(user);
                }
            }

            return updated;
        }

        public static List<DSAccount> ScanUsers(List<DSAccount> users)
        {
            var valids = users
                .Where(u => u.NTHash != null)
                .ToList();

            var matches = ScanUserHashes(valids);
            var results = new List<DSAccount>();

            foreach (var user in valids)
            {
                if (matches.Contains(user.NTHash))
                    results.Add(user);
            }

            return results;
        }

        private static HashSet<byte[]> ScanUserHashes(List<DSAccount> accounts)
        {
            byte[] input = new byte[accounts.Count * 16];

            for (int i = 0; i < accounts.Count; i++)
                accounts[i].NTHash.CopyTo(input, i * 16);

            AddSource(Config.ReadFile("data", "password_file"));
            TestHashes(input, accounts.Count, out byte[] output);
            ClearSources();

            HashSet<byte[]> matches = new HashSet<byte[]>(new Utility.ByteArrayComparer());

            for (int i = 0, c = (output.Length / 16); i < c; i++)
            {
                byte[] hash = new byte[16];
                Array.Copy(output, i * 16, hash, 0, hash.Length);

                matches.Add(hash);
            }

            return matches;
        }
    }
}
