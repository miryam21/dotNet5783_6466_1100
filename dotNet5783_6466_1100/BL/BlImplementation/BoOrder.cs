﻿using BlApi;
using BO;

namespace BlImplementation;
internal class BoOrder : IBoOrder
{
    DalApi.IDal? dal = DalApi.Factory.Get() ?? throw new NullReferenceException("שכבת הגישה לנתונים חסרה");

    public BO.Order GetOrder(int ID)
    {
        if (ID < 0)// test id
            throw new BO.InvalidInputExeption("המזהה אינו בתחום");

        try
        {
            IEnumerable<DO.Order?> orderListDO = dal!.Order.getAll();
            BO.Order orderTempBO = new BO.Order();// create BO Order list
            DO.Order orderTempDO = dal!.Order.GetByID(ID);// create DO Order list
            IEnumerable<DO.OrderItem?> itemsListDO = dal!.OrderItem.GetItemsList(orderTempDO.ID);  // orderItem list of given id          

            // copy details
            orderTempBO.ID = orderTempDO.ID;
            orderTempBO.CustomerName = orderTempDO.CustomerName;
            orderTempBO.CustomerEmail = orderTempDO.CustomerEmail;
            orderTempBO.CustomerAddress = orderTempDO.CustomerAddress;
            orderTempBO.OrderStatus = BlApi.Tools.GetStatus(orderTempDO);
            orderTempBO.OrderDate = orderTempDO.OrderDate;
            orderTempBO.ShipDate = orderTempDO.ShipDate;
            orderTempBO.DeliveryDate = orderTempDO.DeliveryDate;

            var v = (from o in itemsListDO
                     let name = dal.Product.GetByID((int)(o?.ProductID!)).Name
                     select new BO.OrderItem
                     {
                         Name = name,
                         ID = (int)(o?.ID!),
                         ProductID = (int)(o?.ProductID!),
                         Price = o?.Price,
                         Amount = o?.Amount,
                         TotalPrice = o?.Price * o?.Amount
                     }).ToList();
            orderTempBO.Items = v;
            orderTempBO.TotalPrice = BlApi.Tools.GetTotalPrice(itemsListDO!);

            return orderTempBO;// return the order
        }
        catch (DO.DoesntExistException ex)
        {
            throw new BO.DoesntExistException(ex.Message, ex);
        }
        catch (Exception ex) { throw new BO.DoesntExistException(ex.Message, ex); }
    }

    public IEnumerable<BO.OrderForList> getOrderForList()
    {
        IEnumerable<DO.Order?> orderListDO = dal!.Order.getAll();// get order list drom DO

        BO.OrderForList orderForListTemp = new BO.OrderForList();

        return// create the list
            (from orderDO in orderListDO
             let orderFromBL = dal!.OrderItem.GetItemsList((int)(orderDO?.ID!))
             select new BO.OrderForList()
             {
                 ID = (int)(orderDO?.ID!),
                 CustomerName = orderDO?.CustomerName,
                 OrderStatus = Tools.GetStatus((DO.Order)orderDO),
                 AmountOfItems = Tools.GetAmountOfItems(orderFromBL),
                 TotalPrice = Tools.GetTotalPrice(orderFromBL)
             }).ToList().OrderBy(x => x.ID);
    }

    public BO.OrderTracking TrackOrder(int ID)
    {
        if (ID < 0)
            throw new BO.InvalidInputExeption("המזהה אינו בתחום");

        try
        {
            DO.Order orderDO = dal!.Order.GetByID(ID); // order gy id from DO
            List<Tuple<DateTime?, string>>? tempTrackList = new List<Tuple<DateTime?, string>>();// create the list

            if (orderDO.OrderDate != null)
            {
                tempTrackList.Add(Tuple.Create(orderDO.OrderDate, "הזמנה אושרה"));
                if (orderDO.ShipDate != null)
                {
                    tempTrackList.Add(Tuple.Create(orderDO.ShipDate, " הזמנה נשלחה"));
                    if (orderDO.DeliveryDate != null)
                    {
                        tempTrackList.Add(Tuple.Create(orderDO.DeliveryDate, " הזמנה נמסרה"));
                    }
                }
            }
            BO.OrderTracking orderTracking = new BO.OrderTracking()
            {
                ID = orderDO.ID,
                OrderStatus = BlApi.Tools.GetStatus(orderDO),
                trackList = tempTrackList
            };
            return orderTracking;
        }
        catch (DO.DoesntExistException ex)
        {
            throw new BO.DoesntExistException(ex.Message, ex);
        }
    }

    public IEnumerable<OrderForList> GetPartOfOrder(Predicate<OrderForList> check)
    {
        var listToReturn = (from p in getOrderForList()
                            where check(p)
                            select p).ToList<OrderForList>();
        return listToReturn;
    }
    public BO.Order UpdateProvisionOrder(int ID)
    {
        if (ID < 0)
            throw new BO.InvalidInputExeption("המזהה אינו בתחום");

        try
        {
            DO.Order orderDO = dal!.Order.GetByID(ID);// get order by id from DO
            IEnumerable<DO.OrderItem?>? itemsListDO = dal.OrderItem.GetItemsList(orderDO.ID);

            // copy details
            var v = (from o in itemsListDO
                     let name = dal!.Product.GetByID((int)(o?.ProductID!)).Name
                     select new BO.OrderItem
                     {
                         Name = name,
                         ID = (int)(o?.ID!),
                         ProductID = (int)(o?.ProductID!),
                         Price = o?.Price,
                         Amount = o?.Amount,
                         TotalPrice = o?.Price * o?.Amount
                     }).ToList();

            if (orderDO.DeliveryDate != null && orderDO.DeliveryDate < DateTime.Now)
            {
                orderDO.DeliveryDate = DateTime.Now;
                dal.Order.Update(orderDO);
            }
            // create order to return
            BO.Order orderToReturn = new BO.Order()
            {
                ID = orderDO.ID,
                CustomerName = orderDO.CustomerName,
                CustomerEmail = orderDO.CustomerEmail,
                CustomerAddress = orderDO.CustomerAddress,
                OrderStatus = BO.Status.נמסר,
                OrderDate = orderDO.OrderDate,
                ShipDate = orderDO.ShipDate,
                DeliveryDate = DateTime.Now,
                Items = v, //(List<BO.OrderItem?>)Tools.getBOList(dal.OrderItem.GetItemsList(orderDO.ID)),
                TotalPrice = Tools.GetTotalPrice(itemsListDO!)

            };

            DO.Order orderToUpdate = (DO.Order)Tools.CopyPropToStruct(orderToReturn, typeof(DO.Order));// convert BO to DO
            dal.Order.Update(orderToUpdate);// update in DAL
            return orderToReturn;
        }
        catch (DO.DoesntExistException ex)
        {
            throw new BO.DoesntExistException(ex.Message, ex);
        }
    }
    public BO.Order UpdateShipOrder(int ID)
    {
        //test
        if (ID < 0)
            throw new BO.InvalidInputExeption("המזהה אינו בתחום");

        try
        {
            DO.Order orderDO = dal!.Order.GetByID(ID);// get order by id from DAL
            IEnumerable<DO.OrderItem?> itemsListDO = dal.OrderItem.GetItemsList(orderDO.ID);  // orderItem list of order (DO)                                          // copy details
            var v = (from o in itemsListDO
                     let name = dal.Product.GetByID((int)(o?.ProductID!)).Name
                     select new BO.OrderItem
                     {
                         Name = name,
                         ID = (int)(o?.ID!),
                         ProductID = (int)(o?.ProductID!),
                         Price = o?.Price,
                         Amount = o?.Amount,
                         TotalPrice = o?.Price * o?.Amount
                     }).ToList();
            if (orderDO.ShipDate != null && orderDO.ShipDate < DateTime.Now)
            {
                orderDO.ShipDate = DateTime.Now;
                dal.Order.Update(orderDO);
            }
            BO.Order orderToReturn = new BO.Order()
            {
                ID = orderDO.ID,
                CustomerName = orderDO.CustomerName,
                CustomerEmail = orderDO.CustomerEmail,
                CustomerAddress = orderDO.CustomerAddress,
                OrderStatus = BO.Status.נשלח,
                OrderDate = orderDO.OrderDate,
                ShipDate = DateTime.Now,
                DeliveryDate = orderDO.DeliveryDate,
                Items = v,//(List<BO.OrderItem?>)Tools.getBOList(dal.OrderItem.GetItemsList(orderDO.ID)),
                TotalPrice = Tools.GetTotalPrice(itemsListDO!)
            };
            DO.Order orderToUpdate = (DO.Order)Tools.CopyPropToStruct(orderToReturn, typeof(DO.Order));
            dal.Order.Update(orderToUpdate);
            return orderToReturn;
        }
        catch (DO.DoesntExistException ex)
        {
            throw new BO.DoesntExistException(ex.Message, ex);
        }
    }
}
 
