    private bool SYNCLogdataWithCommercient(String TableName, Int32 Version, Int64 TokenNo, String Op, Boolean IsDateTime2)
    {

        int EndIndex = Convert.ToInt32(UserInformation.Get_FlagValue(Flags.BATCH_SIZE.ToString())); //200
        float Checkpoint = 0;

        try
        {
            int failResult;
            Int64 NewToken = -1;

            Checkpoint = 0.01f;
            Checkpoint = 0.001f;

            #region
            Checkpoint = 0.002f;
            do
            {
                Checkpoint = 1;
                if (TokenNo == -1)
                {
                    Checkpoint = 1.11f;
                    try
                    {
                        Ds_LogData = get_LogRecords(TableName, StartIndex, EndIndex, Version, 0, Op, IsDateTime2);
                        if (Ds_LogData == null)
                        {
                            throw new Exception("Dataset object is null");
                        }
                        Checkpoint = 1.12f;
                        if (Ds_LogData.Tables[0].Rows.Count > 0)
                        {
                            NewToken = InsertToken(0);
                            Checkpoint = 1.122f;
                            //Update_LogTableData_with_TokenID(TableName, Ds_LogData.Tables[0], NewToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Table Name : " + TableName + " StartIndex :" +
                            StartIndex + " EndIndex :" + EndIndex + " Version :" + Version + " Op :" + Op + " " + ex.Message, ex.InnerException);
                    }
                    Checkpoint = 1.13f;
                }
                else
                {
                    Checkpoint = 1.21f;
                    NewToken = TokenNo;
                    Ds_LogData = get_LogRecords(TableName, StartIndex, EndIndex, Version, TokenNo, Op, IsDateTime2);
                    Checkpoint = 1.22f;
                }
                Checkpoint = 2;
                if (Ds_LogData.Tables[0].Rows.Count > 0)
                {
                    SendData(Ds_LogData, TableName, Version, NewToken, TokenNo, Op);
                }
                else
                {
                    break;
                }
                Checkpoint = 3;
            } while (Ds_LogData != null);
            Checkpoint = 4;
            #endregion

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("CheckPoint : " + Checkpoint + " MesERP_Salesforce_Clone : " + ex.Message, ex.InnerException);
        }
    }

    private void SendData(DataSet LogData, string TableName, Int32 Version, Int64 NewToken, Int64 TokenNo, String Op, bool IsSingle = false)
    {

        //API Response
        float Checkpoint;
        try
        {
            Checkpoint = 2.11f;
            response = "";
            response = this.CallWebService(LogData, TableName, NewToken, Op);
            Checkpoint = 2.12f;
            if (response == null)   //CallWebService method return null only when no webService is defined to called to SYNC data
            {
                Commit_LogTableData(TableName, LogData.Tables[0], Op);
                Checkpoint = 2.13f;
                // UpdateToken(NewToken, 1);
                Checkpoint = 2.14f;
                // return true;
            }
        }
        catch (Exception ex)
        {
            Checkpoint = 2.15f;
            int failResult = IncrementFailureCounter(TableName, LogData.Tables[0], ex.Message.ToString());
            Checkpoint = 2.16f;
            SyncUtility.WriteEntry("STEP 8: Exception while SYNC Data from Logtable : " + TableName + " Message = " + ex.Message, EventLogEntryType.Error);
            SyncUtility.Log_Error(null, "STEP 8: Exception while SYNC Data from Logtable : " + TableName + " Message = " + ex.Message, "bool SYNC_LogData_With_Commercient(String TableName, Int32 Version, Int64 TokenNo, String Op)", true, UserInformation.SchemaName);

            Commit_LogTableData(TableName, LogData.Tables[0], Op);
        }

        //Deserializes the response received from API 
        try
        {
            Checkpoint = 2.17f;
            tr_co_GID = SyncUtility.Deserialise<CO_ConstantID>(response);
            Checkpoint = 2.18f;
        }
        catch (Exception ex)
        {
            throw new Exception("TableName :" + TableName + " VersionNumber: " + Version + " TokenNo:" + TokenNo + "  Response :" + response + " Option :" + Op + " Exception MesERP_Salesforce_Clone :" + ex.Message, ex.InnerException);
        }

        //Response is SUCCESS
        if (tr_co_GID.ResponseCode.Equals("1") == true)
        {
            Checkpoint = 2.19f;
            Commit_LogTableData(TableName, LogData.Tables[0], Op);
            Checkpoint = 2.20f;
            UpdateToken(NewToken, 1);
            Checkpoint = 2.21f;
            Update_LogTableData_with_TokenID(TableName, LogData.Tables[0], NewToken);
        }

        //Response is FALSE
        if (tr_co_GID.ResponseCode.Equals("1") == false)
        {
            Checkpoint = 2.22f;
            //int failResult = IncrementFailureCounter(TableName, LogData.Tables[0], tr_co_GID.ResponseMsg);
            Checkpoint = 2.23f;

            if (IsSingle)
            {
                if (LogData.Tables[0].Rows.Count > 1)   //After divide by 10 and records more then 1
                {
                    try
                    {
                        DataSet subsetDataSet = new DataSet();  
                        DataTable originalTable = LogData.Tables[0];
                        // Clone the structure of the original DataTable

                        // Copy 1 Rows
                        for (int i = 0; i < originalTable.Rows.Count; i++)
                        {
                            DataTable subsetTable = originalTable.Clone();
                            DataRow row = originalTable.Rows[i];
                            subsetTable.ImportRow(row);
                            
                            // Add the new DataTable to the new DataSet
                            subsetDataSet.Tables.Add(subsetTable);
                            SendData(subsetDataSet, TableName, Version, NewToken, TokenNo, Op, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        SyncUtility.WriteEntry("Error occured during the batch partition - 1 " + ex.Message);
                        SyncUtility.Log_Error(ex, "Error occured during the batch partition - 1" + TableName + " Message = " + ex.Message, "SendData", true, UserInformation.SchemaName);
                    }
                }
                else if (LogData.Tables[0].Rows.Count == 1) //After failed record when sending 1 by 1  records
                {
                    Checkpoint = 2.22f;
                    int failResult = IncrementFailureCounter(TableName, LogData.Tables[0], tr_co_GID.ResponseMsg);
                    Checkpoint = 2.23f;
                    SyncUtility.WriteEntry("STEP 8: Exception while SYNC Data from Logtable : " + TableName + " Message = " + tr_co_GID.ResponseMsg.ToString());
                    SyncUtility.Log_Error(null, "STEP 8: Exception while SYNC Data from Logtable : " + TableName + " Message = " + tr_co_GID.ResponseMsg.ToString(), "bool SYNC_LogData_With_Commercient(String TableName, Int32 Version, Int64 TokenNo, String Op)", true, UserInformation.SchemaName);
                    if (failResult > 4)
                    { }
                    Commit_LogTableData(TableName, LogData.Tables[0], Op);

                    UpdateToken(NewToken, 1);
                    Update_LogTableData_with_TokenID(TableName, LogData.Tables[0], NewToken);
                }
            }
            else
            {
                try
                {
                    int start = 0;
                    int newbatch = LogData.Tables[0].Rows.Count / 10;
                    int end = newbatch;
                    do
                    {
                        // Create a new DataSet to store the subset of rows
                        DataSet subsetDataSet = new DataSet();

                        DataTable originalTable = LogData.Tables[0];
                        // Clone the structure of the original DataTable
                        DataTable subsetTable = originalTable.Clone();

                        // Copy 20 Rows
                        for (int i = start; i < end && i < originalTable.Rows.Count; i++)
                        {
                            DataRow row = originalTable.Rows[i];
                            subsetTable.ImportRow(row);
                        }

                        // Add the new DataTable to the new DataSet
                        subsetDataSet.Tables.Add(subsetTable);
                        SendData(subsetDataSet, TableName, Version, NewToken, TokenNo, Op, true);
                        start += end;
                        end += newbatch;
                    }
                    while (start < Ds_LogData.Tables[0].Rows.Count);

                }
                catch (Exception ex)
                {
                    SyncUtility.WriteEntry("Error occured during the batch partition - 10" + ex.Message, EventLogEntryType.Error);
                    SyncUtility.Log_Error(ex, "Error occured during the batch partition - 10" + TableName + " Message = " + ex.Message, "SendData", true, UserInformation.SchemaName);
                }
            }
        }
        Checkpoint = 2.24f;
    }

