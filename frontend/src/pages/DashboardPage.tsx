import React, { useEffect, useState } from 'react';
import { Button, Card, Col, Row, Select, Statistic, Table, Tag, message } from 'antd';
import { PlayCircleOutlined, PauseCircleOutlined, ReloadOutlined } from '@ant-design/icons';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip as ChartTooltip, Legend, ResponsiveContainer } from 'recharts';
import { dashboardService, runService } from '../api/services';
import type { LogEntry, StatItem, AnalyticsItem } from '../types';
import dayjs from 'dayjs';

const DashboardPage: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [isRunning, setIsRunning] = useState(false);
  const [stats, setStats] = useState<StatItem[]>([]);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [selectedMarketplace, setSelectedMarketplace] = useState<string | undefined>(undefined);
  const [analytics, setAnalytics] = useState<AnalyticsItem[]>([]);

  // Загрузка данных
  const fetchData = async (isBackground: boolean = false) => {
    try {
      if (!isBackground) setLoading(true);
      
      const [statusData, statsData, logsData, analyticsData] = await Promise.all([
        runService.getStatus(),
        dashboardService.getStats(),
        // Передаем выбранный маркетплейс
        dashboardService.getLogs(50, selectedMarketplace),
        dashboardService.getAnalytics() // <--- Новый запрос
      ]);

      setIsRunning(statusData.data.isRunning);
      setStats(statsData.data);
      setLogs(logsData.data);
      setAnalytics(analyticsData.data); // <--- Сохраняем
    } catch (error) {
      console.error(error);
      // Показываем ошибку только если это не фоновое обновление (чтобы не спамить тостами)
      if (!isBackground) message.error('Ошибка загрузки данных');
    } finally {
      if (!isBackground) setLoading(false);
    }
  };

  // Единый эффект для загрузки и таймера
  useEffect(() => {
    // 1. Сразу грузим данные (показываем лоадер, так как фильтр сменился)
    fetchData(false);

    // 2. Запускаем таймер для тихого обновления
    // Он создается заново при каждой смене фильтра, поэтому "видит" актуальный selectedMarketplace
    const interval = setInterval(() => fetchData(true), 3000);

    return () => clearInterval(interval);
  }, [selectedMarketplace]); // <--- Зависимость обязательна!

  // Управление ботом
  const toggleBot = async () => {
    try {
      if (isRunning) {
        await runService.stop();
        message.warning('Команда на остановку отправлена');
      } else {
        await runService.start();
        message.success('Бот запускается...');
      }
      // Сразу обновляем статус
      const status = await runService.getStatus();
      setIsRunning(status.data.isRunning);
    } catch (error) {
      message.error('Не удалось переключить состояние бота');
    }
  };

  // Колонки таблицы
  const columns = [
    {
      title: 'Маркетплейс',
      dataIndex: 'marketplace',
      key: 'marketplace',
      render: (text: string) => (
        <Tag color={text === 'Wildberries' ? 'purple' : 'blue'}>{text}</Tag>
      ),
    },
    {
      title: 'Время',
      dataIndex: 'processedAt',
      key: 'processedAt',
      render: (date: string) => dayjs(date).format('HH:mm:ss DD.MM'),
    },
    {
      title: 'Отзыв',
      dataIndex: 'reviewText',
      key: 'reviewText',
      ellipsis: true,
    },
    {
      title: 'Ответ Нейросети',
      dataIndex: 'generatedResponse',
      key: 'generatedResponse',
      ellipsis: true,
    },
    {
      title: 'Рейтинг',
      dataIndex: 'rating',
      key: 'rating',
      render: (rating: number) => <Tag color={rating >= 4 ? 'green' : 'orange'}>{rating}★</Tag>,
    },
  ];

  return (
    <div>
      {/* Верхняя панель: Статус и Кнопки */}
      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} md={8}>
          <Card loading={loading}>
            <Statistic
              title="Статус Бота"
              value={isRunning ? 'АКТИВЕН' : 'ОСТАНОВЛЕН'}
              valueStyle={{ color: isRunning ? '#3f8600' : '#cf1322' }}
              prefix={isRunning ? <PlayCircleOutlined /> : <PauseCircleOutlined />}
            />
            <Button 
                type={isRunning ? 'default' : 'primary'} 
                danger={isRunning}
                onClick={toggleBot}
                style={{ marginTop: 16, width: '100%' }}
            >
              {isRunning ? 'Остановить работу' : 'Запустить бота'}
            </Button>
          </Card>
        </Col>
        
        {/* Статистика по WB и Ozon */}
        {stats.map((stat) => (
          <Col xs={24} sm={12} md={8} key={stat.marketplace}>
             <Card loading={loading}>
                <Statistic 
                    title={`Ответов в ${stat.marketplace} (сегодня)`} 
                    value={stat.count} 
                    suffix="шт." 
                />
             </Card>
          </Col>
        ))}
      </Row>

      {/* График активности */}
      <Card title="Динамика ответов (7 дней)" style={{ marginBottom: 24 }} bodyStyle={{ padding: 0 }}>
        <div style={{ width: '100%', height: 300 }}>
          <ResponsiveContainer>
            <BarChart data={analytics}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="date" />
              <YAxis />
              <ChartTooltip />
              <Legend />
              <Bar dataKey="wildberries" name="Wildberries" fill="#8884d8" />
              <Bar dataKey="ozon" name="Ozon" fill="#82ca9d" />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </Card>

      {/* Таблица логов */}
      <Card
        title="История активности (Live)"
        extra={
          <div style={{ display: 'flex', gap: 10 }}>
            <Select
              style={{ width: 150 }}
              placeholder="Все магазины"
              allowClear
              value={selectedMarketplace} // <--- ВОТ ЭТО ДОБАВИТЬ
              onChange={(value) => setSelectedMarketplace(value)}
              options={[
                { value: 'Wildberries', label: 'Wildberries' },
                { value: 'Ozon', label: 'Ozon' },
              ]}
            />
            <Button icon={<ReloadOutlined />} onClick={() => fetchData(false)} loading={loading}>
              Обновить
            </Button>
          </div>
        }
      >
        <Table
            dataSource={logs}
            columns={columns}
            rowKey="id"
            pagination={{ pageSize: 10 }}
            size="small"
            loading={loading}
            scroll={{ x: 800 }}
        />
      </Card>
    </div>
  );
};

export default DashboardPage;