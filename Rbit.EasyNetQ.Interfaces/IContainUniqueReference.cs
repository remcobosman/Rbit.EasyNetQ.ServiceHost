namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces
{
    /// <summary>
    /// Implement this interface if you want to have your message written to the audit log.
    /// </summary>
    public interface IContainUniqueReference
    {
        /// <summary>
        /// Returns the unique reference code, usually stored in the bru_autonumber field in CRM.
        /// </summary>
        string UniqueReferenceCode { get; }
    }
}
