﻿using DSInternals.Common.Data;
using DSInternals.Replication.Model;
using DSInternals.Replication.Interop;
using NDceRpc;
using NDceRpc.Microsoft.Interop;
using NDceRpc.Native;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using DSInternals.Common.Interop;
using DSInternals.Common.Cryptography;
using DSInternals.Common;

namespace DSInternals.Replication
{
    public class DirectoryReplicationClient : IDisposable
    {
        /// <summary>
        /// Service principal name (SPN) of the destination server.
        /// </summary>
        private const string ServicePrincipalNameFormat = "ldap/{0}";
        private const string DrsNamedPipeName = @"\pipe\lsass";

        /// <summary>
        /// Identifier of Windows Server 2000 dcpromo.
        /// </summary>
        private static readonly Guid DcPromoGuid2k = new Guid("6abec3d1-3054-41c8-a362-5a0c5b7d5d71");

        /// <summary>
        /// Identifier of Windows Server 2003+ dcpromo.
        /// </summary>
        private static readonly Guid DcPromoGuid2k3 = new Guid("6afab99c-6e26-464a-975f-f58f105218bc");

        /// <summary>
        /// Non-DC client identifier.
        /// </summary>
        private static readonly Guid NtdsApiClientGuid = new Guid("e24d201a-4fd6-11d1-a3da-0000f875ae0d");

        private NativeClient rpcConnection;
        private DrsConnection drsConnection;
        private NamedPipeConnection npConnection;

        public DirectoryReplicationClient(string server, RpcProtocol protocol, NetworkCredential credential = null)
        {
            Validator.AssertNotNullOrWhiteSpace(server, nameof(server));
            this.CreateRpcConnection(server, protocol, credential);
            this.drsConnection = new DrsConnection(this.rpcConnection.Binding, NtdsApiClientGuid);
        }

        public IEnumerable<DSAccount> GetAccounts(string domainNamingContext)
        {
            Validator.AssertNotNullOrWhiteSpace(domainNamingContext, nameof(domainNamingContext));
            ReplicationCookie cookie = new ReplicationCookie(domainNamingContext);
            return GetAccounts(cookie);
        }

        public IEnumerable<DSAccount> GetAccounts(ReplicationCookie initialCookie)
        {
            Validator.AssertNotNull(initialCookie, nameof(initialCookie));
            // Create AD schema
            var schema = BasicSchemaFactory.CreateSchema();
            var currentCookie = initialCookie;
            ReplicationResult result;

            do
            {
                // Perform one replication cycle
                result = this.drsConnection.ReplicateAllObjects(currentCookie);
                
                // Process the returned objects
                foreach (var obj in result.Objects)
                {
                    obj.Schema = schema;
                    if (!obj.IsAccount)
                    {
                        continue;
                    }
                    var account = new DSAccount(obj, this.SecretDecryptor);
                    yield return account;
                }

                // Update the position of the replication cursor
                currentCookie = result.Cookie;
            } while (result.HasMoreData);
        }

        public DSAccount GetAccount(Guid objectGuid)
        {
            var obj = this.drsConnection.ReplicateSingleObject(objectGuid);
            var schema = BasicSchemaFactory.CreateSchema();
            obj.Schema = schema;
            return new DSAccount(obj, this.SecretDecryptor);
        }

        public DSAccount GetAccount(string distinguishedName)
        {
            var obj = this.drsConnection.ReplicateSingleObject(distinguishedName);
            // TODO: Extract?
            var schema = BasicSchemaFactory.CreateSchema();
            obj.Schema = schema;
            return new DSAccount(obj, this.SecretDecryptor);
        }

        public DSAccount GetAccount(NTAccount accountName)
        {
            Guid objectGuid = this.drsConnection.ResolveGuid(accountName);
            return this.GetAccount(objectGuid);
        }

        public DSAccount GetAccount(SecurityIdentifier sid)
        {
            Guid objectGuid = this.drsConnection.ResolveGuid(sid);
            return this.GetAccount(objectGuid);
        }

        private DirectorySecretDecryptor SecretDecryptor
        {
            get
            {
                return new ReplicationSecretDecryptor(this.drsConnection.SessionKey);
            }
        }

        private void CreateRpcConnection(string server, RpcProtocol protocol, NetworkCredential credential = null)
        {
            EndpointBindingInfo binding;
            switch(protocol)
            {
                case RpcProtocol.TCP:
                    binding = new EndpointBindingInfo(RpcProtseq.ncacn_ip_tcp, server, null);
                    break;
                case RpcProtocol.SMB:
                    binding = new EndpointBindingInfo(RpcProtseq.ncacn_np, server, DrsNamedPipeName);
                    if(credential != null)
                    {
                        // Connect named pipe
                        this.npConnection = new NamedPipeConnection(server, credential);
                    }
                    break;
                default:
                    // TODO: Extract as string
                    throw new NotImplementedException("The requested RPC protocol is not supported.");
            }
            this.rpcConnection = new NativeClient(binding);

            NetworkCredential rpcCredential = credential ?? Client.Self;
            string spn = String.Format(ServicePrincipalNameFormat, server);
            this.rpcConnection.AuthenticateAs(spn, rpcCredential, RPC_C_AUTHN_LEVEL.RPC_C_AUTHN_LEVEL_PKT_PRIVACY, RPC_C_AUTHN.RPC_C_AUTHN_GSS_NEGOTIATE);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (this.drsConnection != null)
            {
                this.drsConnection.Dispose();
                this.drsConnection = null;
            }

            if (this.rpcConnection != null)
            {
                this.rpcConnection.Dispose();
                this.rpcConnection = null;
            }

            if(this.npConnection != null)
            {
                this.npConnection.Dispose();
                this.npConnection = null;
            }
        }
    }
}
