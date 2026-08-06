using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Database.Fnl.Sql
{
    /// <summary>
    ///     Thin helper over <see cref="SqlCommand"/> for calling the original R2 stored procedures.
    ///     The original server binds parameters strictly by position and by exact type
    ///     (see COdbcCommand::BindParameter), so every helper here demands an explicit SqlDbType
    ///     and an explicit Size for the string ones.
    /// </summary>
    public static class StoredProcedure
    {
        /// <summary>
        ///     Name of the parameter that carries the procedure's RETURN code
        /// </summary>
        public const string ReturnValueParameterName = "@RETURN_VALUE";

        /// <summary>
        ///     Creates a command for a stored procedure call with the return code parameter already bound
        /// </summary>
        /// <param name="connection">Connection the command is executed on</param>
        /// <param name="name">Procedure name, for example "dbo.UspCertifyUser_CN"</param>
        /// <returns></returns>
        public static SqlCommand Create(SqlConnection connection, string name)
        {
            SqlCommand command = new SqlCommand(name, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Must be the first parameter: the procedures report business errors by RETURN code
            command.Parameters.Add(new SqlParameter(ReturnValueParameterName, SqlDbType.Int)
            {
                Direction = ParameterDirection.ReturnValue
            });

            return command;
        }

        /// <summary>
        ///     Adds an input varchar(size) parameter
        /// </summary>
        public static SqlParameter AddInVarChar(SqlCommand command, string name, int size, string value)
        {
            return AddIn(command, name, SqlDbType.VarChar, size, value);
        }

        /// <summary>
        ///     Adds an input char(size) parameter
        /// </summary>
        public static SqlParameter AddInChar(SqlCommand command, string name, int size, string value)
        {
            return AddIn(command, name, SqlDbType.Char, size, value);
        }

        /// <summary>
        ///     Adds an input int parameter
        /// </summary>
        public static SqlParameter AddInInt(SqlCommand command, string name, int value)
        {
            return AddIn(command, name, SqlDbType.Int, -1, value);
        }

        /// <summary>
        ///     Adds an input smallint parameter
        /// </summary>
        public static SqlParameter AddInSmallInt(SqlCommand command, string name, short value)
        {
            return AddIn(command, name, SqlDbType.SmallInt, -1, value);
        }

        /// <summary>
        ///     Adds an input tinyint parameter
        /// </summary>
        public static SqlParameter AddInTinyInt(SqlCommand command, string name, byte value)
        {
            return AddIn(command, name, SqlDbType.TinyInt, -1, value);
        }

        /// <summary>
        ///     Adds an input bigint parameter
        /// </summary>
        public static SqlParameter AddInBigInt(SqlCommand command, string name, long value)
        {
            return AddIn(command, name, SqlDbType.BigInt, -1, value);
        }

        /// <summary>
        ///     Adds an input bit parameter
        /// </summary>
        public static SqlParameter AddInBit(SqlCommand command, string name, bool value)
        {
            return AddIn(command, name, SqlDbType.Bit, -1, value);
        }

        /// <summary>
        ///     Adds an input real (4-byte single precision) parameter, used for the map coordinates
        /// </summary>
        public static SqlParameter AddInReal(SqlCommand command, string name, float value)
        {
            return AddIn(command, name, SqlDbType.Real, -1, value);
        }

        /// <summary>
        ///     Adds an input datetime parameter, null is sent as DBNull
        /// </summary>
        public static SqlParameter AddInDateTime(SqlCommand command, string name, DateTime? value)
        {
            return AddIn(command, name, SqlDbType.DateTime, -1, value);
        }

        /// <summary>
        ///     Adds an output varchar(size) parameter
        /// </summary>
        public static SqlParameter AddOutVarChar(SqlCommand command, string name, int size)
        {
            return AddOut(command, name, SqlDbType.VarChar, size);
        }

        /// <summary>
        ///     Adds an output char(size) parameter
        /// </summary>
        public static SqlParameter AddOutChar(SqlCommand command, string name, int size)
        {
            return AddOut(command, name, SqlDbType.Char, size);
        }

        /// <summary>
        ///     Adds an output int parameter
        /// </summary>
        public static SqlParameter AddOutInt(SqlCommand command, string name)
        {
            return AddOut(command, name, SqlDbType.Int, -1);
        }

        /// <summary>
        ///     Adds an output smallint parameter
        /// </summary>
        public static SqlParameter AddOutSmallInt(SqlCommand command, string name)
        {
            return AddOut(command, name, SqlDbType.SmallInt, -1);
        }

        /// <summary>
        ///     Adds an output tinyint parameter
        /// </summary>
        public static SqlParameter AddOutTinyInt(SqlCommand command, string name)
        {
            return AddOut(command, name, SqlDbType.TinyInt, -1);
        }

        /// <summary>
        ///     Adds an output bigint parameter
        /// </summary>
        public static SqlParameter AddOutBigInt(SqlCommand command, string name)
        {
            return AddOut(command, name, SqlDbType.BigInt, -1);
        }

        /// <summary>
        ///     Adds an output bit parameter
        /// </summary>
        public static SqlParameter AddOutBit(SqlCommand command, string name)
        {
            return AddOut(command, name, SqlDbType.Bit, -1);
        }

        /// <summary>
        ///     Adds an output real (4-byte single precision) parameter, used for the map coordinates
        /// </summary>
        public static SqlParameter AddOutReal(SqlCommand command, string name)
        {
            return AddOut(command, name, SqlDbType.Real, -1);
        }

        /// <summary>
        ///     Adds an output datetime parameter
        /// </summary>
        public static SqlParameter AddOutDateTime(SqlCommand command, string name)
        {
            return AddOut(command, name, SqlDbType.DateTime, -1);
        }

        /// <summary>
        ///     Reads the RETURN code of the executed procedure
        /// </summary>
        /// <param name="command">Command created by <see cref="Create"/> and already executed</param>
        /// <returns>0 on success, procedure specific code otherwise</returns>
        public static int ReturnValue(SqlCommand command)
        {
            SqlParameter parameter = command.Parameters[ReturnValueParameterName];

            if (parameter.Value == null || parameter.Value == DBNull.Value)
            {
                throw new InvalidOperationException(
                    $"Stored procedure '{command.CommandText}' has no return code; the command was not executed");
            }

            return (int)parameter.Value;
        }

        /// <summary>
        ///     Reads a string parameter, char(N) values come back padded with spaces
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns>Value without trailing spaces or null</returns>
        public static string GetTrimmedString(SqlParameter parameter)
        {
            if (parameter?.Value == null || parameter.Value == DBNull.Value)
            {
                return null;
            }

            return parameter.Value.ToString().TrimEnd();
        }

        /// <summary>
        ///     Adds an input parameter, size is applied only to the string types
        /// </summary>
        /// <param name="command"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="size">Declared size for char/varchar, -1 for the rest</param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static SqlParameter AddIn(SqlCommand command, string name, SqlDbType type, int size, object value)
        {
            SqlParameter parameter = new SqlParameter(name, type)
            {
                Direction = ParameterDirection.Input,
                Value = value ?? (object)DBNull.Value
            };

            if (size >= 0)
            {
                parameter.Size = size;
            }

            command.Parameters.Add(parameter);

            return parameter;
        }

        /// <summary>
        ///     Adds an output parameter, size is applied only to the string types
        /// </summary>
        /// <param name="command"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="size">Declared size for char/varchar, -1 for the rest</param>
        /// <returns></returns>
        private static SqlParameter AddOut(SqlCommand command, string name, SqlDbType type, int size)
        {
            SqlParameter parameter = new SqlParameter(name, type)
            {
                Direction = ParameterDirection.Output
            };

            if (size >= 0)
            {
                parameter.Size = size;
            }

            command.Parameters.Add(parameter);

            return parameter;
        }
    }
}
