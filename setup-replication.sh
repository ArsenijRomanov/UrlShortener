#!/bin/sh

echo "Ждём 40 секунд пока master полностью запустится..."
sleep 40

echo "Проверяем подключение к master..."
until pg_isready -h db-master -p 5432 -U user; do
  echo "Ждём master..."
  sleep 5
done

echo "Очищаем папку данных..."
rm -rf /var/lib/postgresql/data/*

echo "Копируем данные с master..."
PGPASSWORD=simplepass pg_basebackup -h db-master -D /var/lib/postgresql/data -U user -v -P -R -X stream

echo "Запускаем PostgreSQL slave..."
exec postgres