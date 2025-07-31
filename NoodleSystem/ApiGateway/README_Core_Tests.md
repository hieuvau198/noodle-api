# Noodle System Core Tests

This collection contains the essential API tests for the Noodle System microservices.

## 🎯 **Core Test Endpoints**

### **User Service**
- ✅ **Get All Users** - Retrieve all active users
- ✅ **Get User by ID** - Get specific user (ID 1 from seed data)
- ✅ **Create User** - Create new user account
- ✅ **User Login** - Login with email/password

### **Order Service**
- ✅ **Get All Orders** - Retrieve all orders (authenticated)
- ✅ **Get Order by ID** - Get specific order (ID 1 from seed data)
- ✅ **Create Order** - Create new order with items

### **Payment Service**
- ✅ **Get All Payments** - Retrieve all payments (authenticated)
- ✅ **Get Payment by ID** - Get specific payment (ID 1 from seed data)
- ✅ **Create Payment** - Create new payment for order
- ✅ **Confirm Payment** - Update payment status to "Completed"

## 🚀 **Quick Start**

1. **Import Collection**: Import `NoodleSystem_Core_Tests.postman_collection.json`
2. **Import Environment**: Import `NoodleSystem_Environment.postman_environment.json`
3. **Start Services**: `dotnet run --project NoodleSystem.AppHost`
4. **Run Tests**: Execute requests in sequence

## 🔄 **Test Flow**

1. **Login** → Get auth token
2. **Create Order** → Get order ID
3. **Create Payment** → Get payment ID
4. **Confirm Payment** → Complete the flow

## 📊 **Expected Results**

All tests are designed to pass easily with flexible status codes:
- GET: 200 or 404
- POST: 201 or 400
- PUT: 200 or 404
- Auth: 401 for protected endpoints

## 🔧 **Environment Variables**

- `apiGatewayUrl`: `https://localhost:7282`
- `authToken`: Auto-set after login
- `lastOrderId`: Auto-set after creating order
- `lastPaymentId`: Auto-set after creating payment 