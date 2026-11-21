namespace Acme.Bakery.Platform.Sales.Domain.Model.ValueObjects;

// Estados posibles de un pedido (en MySQL se guardan como texto)
public enum EOrderStatus
{
    Pending,  // pendiente: recién creado, aún no se recoge
    Delivered // entregado al cliente
}
