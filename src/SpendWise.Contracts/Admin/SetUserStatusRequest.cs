using System.ComponentModel.DataAnnotations;

namespace SpendWise.Contracts.Admin;

public record SetUserStatusRequest([Required] bool? IsActive);
