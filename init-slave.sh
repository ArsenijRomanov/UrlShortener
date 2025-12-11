#!/bin/sh
set -e

echo "Ждём 30 секунд пока master полностью запустится..."
sleep 30

echo "Проверяем master..."
until pg_isready -h db-master -p 5432 -U user -d url_db; do
    echo "Master не готов, ждём 5 секунд..."
    sleep 5
done

echo "Настраиваем репликацию..."
rm -rf /var/lib/postgresql/data/*

echo "Копируем данные с master..."
PGPASSWORD=replicator_pass pg_basebackup \
  -h db-master \
  -p 5432 \
  -D /var/lib/postgresql/data \
  -U replicator \
  -v \
  -P \
  -R \
  -X stream \
  -S slave_slot1

echo "Настраиваем права..."
chmod 700 /var/lib/postgresql/data

echo "Репликация настроена!"