#include "transaction.h"

namespace NickvisionMoney::Models
{
    Transaction::Transaction(int id) : m_id(id), m_date("1/1/2000"), m_description(""), m_type(TransactionType::Income), m_amount(0.00)
    {

    }

    int Transaction::getID() const
    {
        return m_id;
    }

    void Transaction::setID(int id)
    {
        m_id = id;
    }

    const std::string& Transaction::getDate() const
    {
        return m_date;
    }

    void Transaction::setDate(const std::string& date)
    {
        m_date = date;
    }

    const std::string& Transaction::getDescription() const
    {
        return m_description;
    }

    void Transaction::setDescription(const std::string& description)
    {
        m_description = description;
    }

    TransactionType Transaction::getType() const
    {
        return m_type;
    }

    std::string Transaction::getTypeAsString() const
    {
        if(m_type == TransactionType::Income)
        {
            return "Income";
        }
        else
        {
            return "Expense";
        }
    }

    void Transaction::setType(TransactionType type)
    {
        m_type = type;
    }

    double Transaction::getAmount() const
    {
        return m_amount;
    }

    void Transaction::setAmount(double amount)
    {
        m_amount = amount;
    }

    bool Transaction::operator<(const Transaction& toCompare) const
    {
        return m_id < toCompare.m_id;
    }

    bool Transaction::operator>(const Transaction& toCompare) const
    {
        return m_id > toCompare.m_id;
    }

    bool Transaction::operator==(const Transaction& toCompare) const
    {
        return m_id == toCompare.m_id;
    }

    bool Transaction::operator!=(const Transaction& toCompare) const
    {
        return m_id != toCompare.m_id;
    }
}
