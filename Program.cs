using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;
using System.Data;
using System.IO;

namespace Oracle_Dead_Lock_SIM
{
    class Program
    {
        static void Main(string[] args)
        {
            var eqHmiId = 2195;
            var ipAdr = "DEADLOCK_TEST";

            var sb = new OracleConnectionStringBuilder();
            sb.UserID = "TO_FILL";
            sb.Password = "TO_FILL";
            sb.DataSource = @"(DESCRIPTION =
                                (ADDRESS = (PROTOCOL = TCP)(HOST = 192.168.1.34)(PORT = 1521))
                                (CONNECT_DATA =
                                  (SERVER = DEDICATED)
                                  (SERVICE_NAME = SDMS)
                                )
                              )";
            sb.Pooling = true;
            sb.MinPoolSize = 5;
            sb.MaxPoolSize = 335;
            sb.DecrPoolSize = 3;
            sb.IncrPoolSize = 5;
            sb.ConnectionTimeout = 25;
            sb.ConnectionLifeTime = 0;
            sb.ValidateConnection = false;
            sb.HAEvents = false;
            sb.LoadBalancing = false;
            sb.MetadataPooling = true;
            sb.StatementCachePurge = false;
            
            for (int i = 0; i < 100; i++)
            {
                Execute(sb.ToString(), eqHmiId, ipAdr, "BARCODE.XML");
                Execute(sb.ToString(), eqHmiId, ipAdr, "OUT.XML");
                Execute(sb.ToString(), eqHmiId, ipAdr, "IN.XML");
                
            }


        }


        static void Execute(string conString, int eqHmiId, string ipAdr, string path)
        {

            var oracleConnection = new OracleConnection(conString);

            if (oracleConnection.State != ConnectionState.Open)
                oracleConnection.Open();

            var OrclCmd = oracleConnection.CreateCommand();

            var blobFile = File.ReadAllBytes(path);

            OracleBlob orclBlob = new OracleBlob(oracleConnection);

            orclBlob.BeginChunkWrite();
            orclBlob.Write(blobFile, 0, blobFile.Length);

            orclBlob.EndChunkWrite();

            OrclCmd.CommandType = CommandType.StoredProcedure;
            OrclCmd.CommandText = "PKG_SRV.P_ADD_ORDER_RESULT";


            OrclCmd.Parameters.Clear();
            OrclCmd.Parameters.Add("inHmiId", OracleDbType.Int32, eqHmiId, ParameterDirection.Input);
            OrclCmd.Parameters.Add("inBarcode", OracleDbType.Blob, orclBlob, ParameterDirection.Input);
            OrclCmd.Parameters.Add("inIP", OracleDbType.Varchar2, ipAdr, ParameterDirection.Input);
            OrclCmd.ExecuteNonQuery();

            //oracleConnection.Close();
        }
    }
}
