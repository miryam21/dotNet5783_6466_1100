﻿using BlApi;
using BO;

namespace BlImplementation;

internal class BoProduct :IBoProduct
{
    DalApi.IDal? dal = DalApi.Factory.Get() ?? throw new NullReferenceException("שכבת הגישה לנתונים חסרה");
    
    /// <summary>
    /// function- adding product to data source
    /// </summary>
    /// <param name="product"></param>
    /// <exception cref="BO.InvalidInputExeption"></exception>
    /// <exception cref="BO.AlreadyExistExeption"></exception>
    public int AddProduct(BO.Product? product)
    {
        //testing 
        if (!(product?.ID >= 100000 && product?.ID < 1000000))// id test
           throw new BO.InvalidInputExeption("המזהה אינו בתחום");
  
        if (product?.Name == null)// name test 
            throw new BO.InvalidInputExeption("השם אינו נכון");

        if (product?.Price <= 0)// price test
            throw new BO.InvalidInputExeption("המחיר אינו יכול להיות שלילי");

        if (product?.InStock < 0)// stock test
            throw new BO.InvalidInputExeption("המלאי אינו יכול להיות שלילי");
        try
        {
            DO.Product productTempDO = new DO.Product()// create DO product to enter the DAL
            {//copy from given product
                ID = product!.ID,
                Name = product?.Name,
                Price = product?.Price,
                Category = (DO.Category?)product?.Category,
                InStock = product?.InStock,
                Path= product?.Path,
            };
           int id= dal!.Product.Add(productTempDO); // adding to DAL
            return id;
        }
        catch (DO.AlreadyExistExeption ex)// if doesnt work catch exeption
        {
            throw new BO.AlreadyExistExeption(ex.Message, ex);
        } 
    }

    /// <summary>
    /// function- delete product from DAL
    /// </summary>
    /// <param name="IDProduct"></param>
    /// <exception cref="BO.DoesntExistException"></exception>
    /// <exception cref="CantDeleteItem"></exception>
    public void DeledeProduct(int IDProduct)
    {
        IEnumerable<DO.Order?> tempList = dal!.Order.getAll();// create temp list to get הכל orders from DAL
        foreach (DO.Order? o in tempList )// go over the list of orders
        {
            List<DO.OrderItem?> itemsInO = new List<DO.OrderItem?>();// create orderItem list for testing
            try
            {
                itemsInO = (List<DO.OrderItem?>)dal.OrderItem.GetItemsList((int)(o?.ID!));
                if(itemsInO.Find((x => x?.ProductID == IDProduct))!=null)// product was found in order
                    throw new BO.CantDeleteItemException("בלתי ניתן למחיקה- המוצר קיים בהזמנה");
            }
            catch (BO.CantDeleteItemException ex)// exeption for product in order case
            {
                throw new BO.CantDeleteItemException(ex.Message, ex);
            }
        }
        try
        {
            dal.Product.Delete(IDProduct); // deleting from DAL
        }
        catch (DO.DoesntExistException ex)// if doesnt work catch exeption
        {
            throw new BO.DoesntExistException(ex.Message, ex);
        }
    }

    /// <summary>
    /// funcion- returns product from DAL by ID
    /// </summary>
    /// <param name="ID"></param>
    /// <returns></returns>
    /// <exception cref="InvalidInputExeption"></exception>
    /// <exception cref="BO.DoesntExistException"></exception>
    public BO.Product? GetProductbyId(int ID)
    {
        if(ID < 0)// test id
            throw new BO.InvalidInputExeption("המזהה אינו בתחום");
        
        try
        {
            BO.Product productTempBO = new BO.Product();// create BO product
            DO.Product productTempDO = dal!.Product.GetByID(ID);// create DO product
            // copy details
            productTempBO.ID = productTempDO.ID;
            productTempBO.Name = productTempDO.Name;
            productTempBO.Price = productTempDO.Price;
            productTempBO.Category = (BO.Category?)productTempDO.Category;
            productTempBO.InStock = productTempDO.InStock;
            productTempBO.Path =productTempDO.Path;

            return productTempBO;// return the product
        }
        catch(DO.DoesntExistException ex)
        {
            throw new BO.DoesntExistException(ex.Message, ex);
        }  
    }

    public BO.ProductItem GetProductByIDAndCart(int ID, BO.Cart? cart)
    {//testing
        if (ID<0)
            throw new BO.InvalidInputExeption("המזהה אינו בתחום");
        
        try
        {
            DO.Product? productTempDO = dal?.Product.GetByID(ID);
            BO.ProductItem productItemBO = new BO.ProductItem()
            {
                ID = (int)(productTempDO?.ID!),
                Name = productTempDO?.Name,
                Price = productTempDO?.Price,
                Category = (BO.Category?)productTempDO?.Category,
                InStock = Tools.inStock((int)productTempDO?.InStock!),
                Amount = Tools.getAmountOfProduct( cart?.Items!, (int)(productTempDO?.ID!)),
                Path=productTempDO?.Path
                
            };
            return productItemBO;

        }
        catch (DO.DoesntExistException ex)
        {
            throw new BO.DoesntExistException(ex.Message, ex);
        }
    }

    /// <summary>
    /// function- returns list of ProductForList 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<BO.ProductForList> getProductForList()
    {
        IEnumerable<DO.Product?> productListDO = dal!.Product.getAll();

        return (from p in productListDO
                let productFromBL = GetProductbyId((int)(p?.ID!))
                select new BO.ProductForList()
                {
                    ID = (int)(p?.ID!),
                    Name = p?.Name,
                    Price = p?.Price,
                    Category = (BO.Category?)p?.Category,
                    Path=p?.Path
                }).ToList().OrderBy(x => x.ID);
    }

    public IEnumerable<ProductForList> GetPartOfProduct(Predicate<ProductForList> check)
    {       
        var listToReturn = (from p in getProductForList()
                     where check(p)
                     select p).ToList<ProductForList>();

        return listToReturn;  
    }

    public void UpdateDetailProduct(BO.Product? product)
    {
        if (!(product?.ID >= 100000 && product?.ID < 1000000))// id test
            throw new BO.InvalidInputExeption("המזהה אינו בתחום");

        if (product?.Name == "")// name test 
            throw new BO.InvalidInputExeption("השם אינו נכון");

        if (product?.Price <= 0)// price test
            throw new BO.InvalidInputExeption("המחיר אינו יכול להיות שלילי");

        if (product?.InStock < 0)// stock test
            throw new BO.InvalidInputExeption("המלאי אינו יכול להיות שלילי");

        try
        {
            DO.Product productTempDO = new DO.Product()// create DO product to update in DAL
            {
                ID = product!.ID,
                Name = product?.Name,
                Price = product?.Price,
                Category = (DO.Category?)product?.Category,
                InStock = product?.InStock,
                Path = product?.Path
            };

            dal!.Product.Update(productTempDO); // updating
        }
        catch (DO.DoesntExistException ex)// if doesnt work catch exeption
        {
            throw new BO.DoesntExistException(ex.Message, ex);
        }
    }
}