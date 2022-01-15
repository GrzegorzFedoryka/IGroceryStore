﻿using IGroceryStore.UserBasket.Core.Exceptions;

namespace IGroceryStore.UserBasket.Core.ValueObjects;

public record UserId
{
    public Guid Value { get; }

    public UserId(Guid value)
    {
        if (value == Guid.Empty) throw new InvalidUserIdException(value);
        Value = value;
    }
    
    public static implicit operator Guid(UserId id) => id.Value;
    public static implicit operator UserId(Guid value) => new(value);
    
}