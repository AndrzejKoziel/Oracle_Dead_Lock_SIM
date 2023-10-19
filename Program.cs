using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Oracle_Dead_Lock_SIM
{
    class Program
    {
        static void Main(string[] args)
        {
            var eqHmiId = 2695;
            var ipAdr = "DEADLOCK_TEST";

            var connectionString = new OracleConnectionStringBuilder();
            connectionString.UserID = "TO_FILL";
            connectionString.Password = "TO_FILL";
            connectionString.DataSource = @"(DESCRIPTION =
                                (ADDRESS = (PROTOCOL = TCP)(HOST = 192.168.1.34)(PORT = 1521))
                                (CONNECT_DATA =
                                  (SERVER = DEDICATED)
                                  (SERVICE_NAME = SDMS)
                                )
                              )";
            connectionString.Pooling = true;
            connectionString.MinPoolSize = 5;
            connectionString.MaxPoolSize = 35;
            connectionString.DecrPoolSize = 3;
            connectionString.IncrPoolSize = 5;
            connectionString.ConnectionTimeout = 25;
            connectionString.ConnectionLifeTime = 0;
            connectionString.ValidateConnection = false;
            connectionString.HAEvents = false;
            connectionString.LoadBalancing = false;
            connectionString.MetadataPooling = true;
            connectionString.StatementCachePurge = false;

            ExecuteSQL(connectionString.ToString(), @"sql\Delete_sn_results.sql", eqHmiId);
            ExecuteSQL(connectionString.ToString(), @"sql\update_in_prod_hist.sql", eqHmiId);

            ProccessTel(connectionString.ToString(), eqHmiId, ipAdr, @"tel\in_11.XML");
            ProccessTel(connectionString.ToString(), eqHmiId, ipAdr, @"tel\Barcode_11.XML");

            Task.Run(() => ProccessTel(connectionString.ToString(), eqHmiId, ipAdr, @"tel\Out_11.XML"));
            Task.Run(() => ProccessTel(connectionString.ToString(), eqHmiId, ipAdr, @"tel\In_12.XML"));

            Parallel.For(0, 100, index =>
            {
                ProccessTel(connectionString.ToString(), eqHmiId, ipAdr, @"tel\Out_11.XML");
                ProccessTel(connectionString.ToString(), eqHmiId, ipAdr, @"tel\In_12.XML");
            });

        }


        static void ProccessTel(string conString, int eqHmiId, string ipAdr, string path)
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

           oracleConnection.Close();
        }
        static void ExecuteSQL(string conString, string path, int eqHmiId)
        {

            var oracleConnection = new OracleConnection(conString);

            if (oracleConnection.State != ConnectionState.Open)
                oracleConnection.Open();

            var OrclCmd = oracleConnection.CreateCommand();

            var sqlText = File.ReadAllText(path);

            OrclCmd.CommandType = CommandType.Text;
            OrclCmd.CommandText = sqlText;

            OrclCmd.Parameters.Clear();
            OrclCmd.Parameters.Add("inEqHmiId", OracleDbType.Int32, eqHmiId, ParameterDirection.Input);
            OrclCmd.ExecuteNonQuery();

            oracleConnection.Close();
        }
    }
}
