
# 🛒 GESTION_PANIER

Application web développée avec **ASP.NET Core Razor Pages** permettant la gestion des produits, catégories, panier et authentification par cookies avec rôles.

---

## 🚀 Fonctionnalités

- 🔐 Authentification personnalisée par cookies
- 👤 Gestion des rôles (Admin / User / Manager)
- 🛍️ Gestion des produits (CRUD)
- ⚡ Récupération optimisée des produits avec mise en cache
- 📦 Gestion des catégories(CRUD)
- 🛒 Panier utilisateur
- 🔒 Accès sécurisé aux pages Admin
- 

---

## 🛠️ Technologies utilisées

- ASP.NET Core Razor Pages
- Entity Framework Core
- SQL Server
- Cookie Authentication
- Gestion du panier par Cookies
- HTML / CSS / Bootstrap

---
## 🔑 Informations pour tester l’application
Compte Admin (prêt à l’emploi)

Email : admin@example.com

Mot de passe : Admin123!

Rôle : Admin

Accès complet à toutes les pages, y compris la création, modification et suppression des produits.

Utilisateurs visiteurs

Peuvent accéder directement à :

/Products/PublicIndex → voir tous les produits

Panier : ajouter et visualiser des produits

Peuvent créer leur propre compte via la page Register.

Par défaut, les nouveaux comptes sont attribués au rôle User.
## ⚙️ Installation et configuration

### 1️⃣ Cloner le projet
```bash
git clone https://github.com/Salma-alloun/GESTION_PANIER.git
### Auteur
Salma Alloun
Update README: ajout du cache pour la récupération des produits

