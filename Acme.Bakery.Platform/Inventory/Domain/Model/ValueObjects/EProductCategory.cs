namespace Acme.Bakery.Platform.Inventory.Domain.Model.ValueObjects;

// Categorías permitidas para un producto (en MySQL se guardan como texto)
public enum EProductCategory
{
    Bread,  // pan
    Cake,   // torta
    Pastry, // pastel / bollería
    Cookie  // galleta
}
