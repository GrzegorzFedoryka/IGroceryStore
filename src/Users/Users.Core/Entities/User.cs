﻿using IGroceryStore.Shared.Abstraction.Common;
using IGroceryStore.Shared.Services;
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

    private PasswordHash _passwordHash;
    private List<string> _refreshTokens;
    private ushort _accessFailedCount;
    private DateTime _lockoutEnd;
    public UserId Id { get; }
    public FirstName FirstName { get; private set; }
    public LastName LastName { get; private set; }
    public Email Email { get; private set; }
    public bool TwoFactorEnabled { get; private set; } = false;
    public bool EmailConfirmed { get; private set; }
    public bool LockoutEnabled { get; private set; }

    //TODO: generate
    public string ConcurrencyStamp { get; private set; } = "";
    public string SecurityStamp { get; private set; } = "";
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
        throw new NotImplementedException();
    }
    
    private void ConfirmEmail()
    {
        EmailConfirmed = true;
        throw new NotImplementedException();
    }

    private void Lock()
    {
        LockoutEnabled = true;
        _lockoutEnd = DateTime.Now.AddMinutes(5);
    }
    
    internal void Unlock()
    {
        LockoutEnabled = false;
        _accessFailedCount = 0;
    }
    
    internal bool Login(string password)
    {
        if (!HashingService.ValidatePassword(password, _passwordHash.Value))
        {
            _accessFailedCount++;
            return false;
        }
        if (_accessFailedCount <= 5) return true;
        Lock();
        return false;
    }

    internal void AddRefreshToken(string refreshToken)
    {
        _refreshTokens ??= new List<string>();
        _refreshTokens.Add(refreshToken);
    }

    public bool TokenExist(string refreshToken)
    {
        return _refreshTokens.Contains(refreshToken);
    }

    public void UpdateRefreshToken(string oldRefreshToken, string newRefreshToken)
    {
        _refreshTokens.Remove(oldRefreshToken);
        _refreshTokens.Add(newRefreshToken);
    }
}