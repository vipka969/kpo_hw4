using PaymentsService.Application.DTOs;
using PaymentsService.Application.Interfaces;
using PaymentsService.Models.Entities;

namespace PaymentsService.Application.UseCases.GetAccount;

public class GetAccountHandler
{
    private readonly IAccountRepository _repository;

    public GetAccountHandler(IAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<AccountResponse?> Handle(Guid userId)
    {
        Account? account = await _repository.GetByUserIdAsync(userId);
        if (account == null)
        {
            return null;
        }

        return AccountResponse.FromDomain(account);
    }
}