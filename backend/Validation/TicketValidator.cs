using TicketFlow.Api.Models;

public static class TicketValidator
{
    public static IResult? Validate(Ticket ticket)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(ticket.Title))
        {
            errors["title"] = ["標題為必填。"];
        }
        else if (ticket.Title.Trim().Length > 200)
        {
            errors["title"] = ["標題最多 200 個字元。"];
        }

        if (string.IsNullOrWhiteSpace(ticket.Description))
        {
            errors["description"] = ["描述為必填。"];
        }
        else if (ticket.Description.Trim().Length > 2000)
        {
            errors["description"] = ["描述最多 2000 個字元。"];
        }

        if (ticket.Assignee.Trim().Length > 120)
        {
            errors["assignee"] = ["指派對象最多 120 個字元。"];
        }

        return errors.Count == 0
            ? null
            : Results.BadRequest(new ValidationErrorResponse("請修正工單欄位後再送出。", errors));
    }
}
