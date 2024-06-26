﻿using DO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DalApi;
/// <summary>
///  interface for implement orderItem
/// </summary>
public interface IOrderItem:ICrud<OrderItem>
{
    IEnumerable<OrderItem?> GetItemsList(int orderId);
     OrderItem GetProductByOrderAndID(int orderId, int productId);
}
