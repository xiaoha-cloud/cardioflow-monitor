using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public interface IAlertService
{
    void AddAlert(AlertMessage alert);
    IReadOnlyList<AlertMessage> GetLatest(int count);
    IReadOnlyList<AlertMessage> GetLatestByRecord(int count, string recordId);
    IReadOnlyList<AlertMessage> GetAll();
    AlertMessage? GetLastAlert();
    int GetCount();
}
