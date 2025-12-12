#!/bin/sh
set -e

echo "Настраиваем master для репликации..."
sleep 5

# Ждём пока PostgreSQL запустится
until pg_isready -U user; do
    echo "PostgreSQL не готов, ждём..."
    sleep 1
done

echo "Правила доступа настроены..."

# Настраиваем параметры репликации
psql -v ON_ERROR_STOP=1 --username "user" --dbname "url_db" <<-EOSQL
    -- Создаём пользователя для репликации
    CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD 'replicator_pass';
    
    -- Создаём слот репликации
    SELECT pg_create_physical_replication_slot('slave_slot1');
    
    -- Настраиваем параметры репликации
    ALTER SYSTEM SET wal_level = replica;
    ALTER SYSTEM SET max_wal_senders = 10;
    ALTER SYSTEM SET max_replication_slots = 10;
    ALTER SYSTEM SET wal_keep_size = 1024;
    ALTER SYSTEM SET hot_standby = on;
    ALTER SYSTEM SET listen_addresses = '*';
EOSQL

# Создаём правильный pg_hba.conf
cat > /var/lib/postgresql/data/pg_hba.conf << EOF
# TYPE  DATABASE        USER            ADDRESS                 METHOD
local   all             all                                     trust
host    all             all             127.0.0.1/32            trust
host    all             all             ::1/128                 trust
host    replication     replicator      0.0.0.0/0               md5
host    all             all             0.0.0.0/0               md5
EOF

# Перезагружаем конфигурацию
psql -U user -d url_db -c "SELECT pg_reload_conf();"

echo "Настройка master завершена!"