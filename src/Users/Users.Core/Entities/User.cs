﻿using System.Net;
using IGroceryStore.Shared.Abstraction.Common;
using IGroceryStore.Shared.Abstraction.Exceptions;
using IGroceryStore.Shared.Services;
using IGroceryStore.Shared.ValueObjects;
using IGroceryStore.Users.Core.Exceptions;
using IGroceryStore.Users.Core.ValueObjects;

namespace IGroceryStore.Users.Core.Entities;

public class User : AuditableEntity
{
    public User()
    {
    }

    internal User(UserId id,
        FirstName firstName,
        LastName lastName,
        Email email,
        PasswordHash passwordHash)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        _passwordHash = passwordHash;
    }

    private const int MaxLoginTry = 5;
    private PasswordHash _passwordHash;
    private List<RefreshToken> _refreshTokens;
    private ushort _accessFailedCount;
    private DateTime _lockoutEnd;
    public UserId Id { get; }
    public FirstName FirstName { get; private set; }
    public LastName LastName { get; private set; }
    public Email Email { get; private set; }
    public bool TwoFactorEnabled { get; private set; } = false;
    public bool EmailConfirmed { get; private set; }
    public bool LockoutEnabled { get; private set; }
    private void UpdatePassword(string password, string oldPassword)
    {
        if (!HashingService.ValidatePassword(oldPassword, _passwordHash.Value))
        {
            _accessFailedCount++;
            throw new IncorrectPasswordException();
        }
        _passwordHash = HashingService.HashPassword(password);
    }
    
    private void UpdateEmail(string email)
    {
        Email = email;
    }
    
    private void ConfirmEmail()
    {
        EmailConfirmed = true;
    }

    internal bool EnableTwoTwoFactor()
    {
        TwoFactorEnabled = true;
        throw new NotImplementedException();
        return true;
    }

    private void Lock()
    {
        LockoutEnabled = true;
        _lockoutEnd = DateTime.Now.AddMinutes(5);
    }
    
    private void Unlock()
    {
        LockoutEnabled = false;
        _accessFailedCount = 0;
    }

    private bool TryUnlock()
    {
        if (_lockoutEnd > DateTime.Now) return false;
        Unlock();
        return true;
    }
    
    internal bool Login(string password)
    {
        if (!TryUnlock()) throw new LoggingTriesExceededException(MaxLoginTry);
        
        if (!HashingService.ValidatePassword(password, _passwordHash.Value))
        {
            _accessFailedCount++;
            return false;
        }
        if (_accessFailedCount <= MaxLoginTry) return true;
        Lock();
        return false;
    }

    internal void AddRefreshToken(RefreshToken refreshToken)
    {
        _refreshTokens ??= new List<RefreshToken>();
        _refreshTokens.Add(refreshToken);
    }

    public bool TokenExist(RefreshToken refreshToken)
        => _refreshTokens.Exists(x => x.Equals(refreshToken));
    public bool TokenExist(string token)
        => _refreshTokens.Exists(x => x.Value == token);
    
    public void UpdateRefreshToken(string oldTokenValue, string newTokenValue)
    {
        var token = _refreshTokens.First(x => x.Value == oldTokenValue);
        var newToken = token with {Value = newTokenValue};
        _refreshTokens.RemoveAll(x => x.Value == oldTokenValue);
        _refreshTokens.Add(newToken);
    }

    public void TryRemoveOldRefreshToken(string userAgent)
    {
        _refreshTokens ??= new List<RefreshToken>();
        if (!_refreshTokens.Exists(x => x.UserAgent == userAgent)) return;
        _refreshTokens.RemoveAll(x => x.UserAgent == userAgent);
    }
    
    
}

internal class LoggingTriesExceededException : GroceryStoreException
{
    public LoggingTriesExceededException(int maxLoginTry) : base("Try again after 5 min")
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.Forbidden;
}