using PaymentsService.Application.DTOs;
using PaymentsService.Application.Interfaces;
using PaymentsService.Models.Entities;

namespace PaymentsService.Application.UseCases.Deposit;

public class DepositHandler
{
    private readonly IAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DepositHandler(IAccountRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountResponse?> Handle(Guid userId, DepositRequest request)
    {
        Account? account = await _repository.GetByUserIdAsync(userId);
        if (account == null)
        {
            throw new InvalidOperationException("Account not found");
        }

        account.Deposit(request.Amount);
        await _repository.UpdateAsync(account);
        await _unitOfWork.SaveChangesAsync();

        return AccountResponse.FromDomain(account);
    }
}