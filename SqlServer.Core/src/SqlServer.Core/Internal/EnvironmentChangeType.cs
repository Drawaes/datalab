namespace SqlServer.Core.Internal
{
    internal enum EnvironmentChangeType : byte
    {
        Database = 1,
        Language = 2,
        CharaterSet = 3,
        PacketSize = 4,
        UnicodeSorting = 5,
        UnicodeSortingComparisonFlags = 6,
        SqlCollation = 7,
        BeginTransaction = 8,
        CommitTransaction = 9,
        RollbackTransaction = 10,
        EnlistDtcTransaction = 11,
        DefectTransaction = 12,
        RealTimeLogShipping = 13,
        PromoteTransaction = 15,
        TransactionManagerAddress = 16,
        TransactionEnded = 17,
        ResetConnectionAck = 18,
        NameOfUserInstance = 19,
        ClientRoutingInformation = 20,
    }
}
