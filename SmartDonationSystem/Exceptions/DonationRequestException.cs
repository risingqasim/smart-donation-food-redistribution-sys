namespace SmartDonationSystem.Exceptions
{
    /// <summary>
    /// Base exception for donation request-related errors
    /// </summary>
    public class DonationRequestException : Exception
    {
        public DonationRequestException(string message) : base(message) { }
        public DonationRequestException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when donation request validation fails
    /// </summary>
    public class DonationRequestValidationException : DonationRequestException
    {
        public List<string> ValidationErrors { get; }

        public DonationRequestValidationException(string message, List<string> validationErrors) 
            : base(message)
        {
            ValidationErrors = validationErrors;
        }
    }

    /// <summary>
    /// Exception thrown when donation request is not found
    /// </summary>
    public class DonationRequestNotFoundException : DonationRequestException
    {
        public int RequestId { get; }

        public DonationRequestNotFoundException(int requestId) 
            : base($"Donation request with ID {requestId} was not found.")
        {
            RequestId = requestId;
        }
    }

    /// <summary>
    /// Exception thrown when donation request operation is not allowed
    /// </summary>
    public class DonationRequestOperationNotAllowedException : DonationRequestException
    {
        public string CurrentStatus { get; }
        public string Operation { get; }

        public DonationRequestOperationNotAllowedException(string operation, string currentStatus) 
            : base($"Operation '{operation}' is not allowed for donation request with status '{currentStatus}'.")
        {
            Operation = operation;
            CurrentStatus = currentStatus;
        }
    }

    /// <summary>
    /// Exception thrown when duplicate donation request is attempted
    /// </summary>
    public class DuplicateDonationRequestException : DonationRequestException
    {
        public int DonationId { get; }
        public int NGOId { get; }

        public DuplicateDonationRequestException(int donationId, int ngoId) 
            : base($"A donation request already exists for donation {donationId} by NGO {ngoId}.")
        {
            DonationId = donationId;
            NGOId = ngoId;
        }
    }
}

