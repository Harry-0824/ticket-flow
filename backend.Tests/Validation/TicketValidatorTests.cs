using TicketFlow.Api.Models;

namespace TicketFlow.Api.Tests.Validation;

public class TicketValidatorTests
{
    [Fact]
    public void Validate_WithValidTicket_ReturnsNull()
    {
        var ticket = new Ticket
        {
            Title = "Login issue",
            Description = "User cannot sign in from the support portal.",
            Assignee = "Alex"
        };

        var result = TicketValidator.Validate(ticket);

        Assert.Null(result);
    }

    [Fact]
    public void Validate_WithInvalidTicket_ReturnsBadRequestResult()
    {
        var ticket = new Ticket
        {
            Title = "",
            Description = " ",
            Assignee = new string('a', 121)
        };

        var result = TicketValidator.Validate(ticket);

        Assert.NotNull(result);
    }
}
