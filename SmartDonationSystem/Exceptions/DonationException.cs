namespace SmartDonationSystem.Exceptions
{
    /// <summary>
    /// Base exception for donation-related errors
    /// </summary>
    public class DonationException : Exception
    {
        public DonationException(string message) : base(message) { }
        public DonationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when donation validation fails
    /// </summary>
    public class DonationValidationException : DonationException
    {
        public List<string> ValidationErrors { get; }

        public DonationValidationException(string message, List<string> validationErrors) 
            : base(message)
        {
            ValidationErrors = validationErrors;
        }
    }

    /// <summary>
    /// Exception thrown when donation is not found
    /// </summary>
    public class DonationNotFoundException : DonationException
    {
        public int DonationId { get; }

        public DonationNotFoundException(int donationId) 
            : base($"Donation with ID {donationId} was not found.")
        {
            DonationId = donationId;
        }
    }

    /// <summary>
    /// Exception thrown when donation operation is not allowed
    /// </summary>
    public class DonationOperationNotAllowedException : DonationException
    {
        public string CurrentStatus { get; }
        public string Operation { get; }

        public DonationOperationNotAllowedException(string operation, string currentStatus) 
            : base($"Operation '{operation}' is not allowed for donation with status '{currentStatus}'.")
        {
            Operation = operation;
            CurrentStatus = currentStatus;
        }
    }

    /// <summary>
    /// Exception thrown when user doesn't have permission to perform operation
    /// </summary>
    public class DonationPermissionException : DonationException
    {
        public DonationPermissionException(string message) : base(message) { }
    }
}

