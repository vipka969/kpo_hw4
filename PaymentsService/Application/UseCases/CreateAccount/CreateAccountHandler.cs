using PaymentsService.Application.DTOs;
using PaymentsService.Application.Interfaces;
using PaymentsService.Models.Entities;

namespace PaymentsService.Application.UseCases.CreateAccount;

public class CreateAccountHandler
{
    private readonly IAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccountHandler(IAccountRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountResponse?> Handle(Guid userId, CreateAccountRequest request)
    {
        bool flag = await _repository.ExistsForUserAsync(userId);
        if (flag)
        {
            throw new InvalidOperationException("User already has an account");
        }

        Account account = Account.Create(userId, request.Balance);
        await _repository.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();

        return AccountResponse.FromDomain(account);
    }
}