namespace HomeManager.Services;

public enum PasswordChangeStatus
{
    Success,
    NotFound,
    // 今のパスワードが違う
    WrongPassword,
    // 新しいパスワードが決まりに合わない (短い・今と同じ)
    InvalidPassword
}
