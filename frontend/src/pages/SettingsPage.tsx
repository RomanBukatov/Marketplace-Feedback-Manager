import React, { useEffect, useState } from 'react';
import { Button, Card, Form, Input, InputNumber, message, Divider, Rate, Modal, Row, Col } from 'antd';
import { SaveOutlined, PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import { settingsService, authService } from '../api/services';
import type { AppSettings } from '../types';

const { TextArea } = Input;

interface PasswordChangeValues {
  oldPassword: string;
  newPassword: string;
}

const SettingsPage: React.FC = () => {
  const [loading, setLoading] = useState(false);
  // ИСПОЛЬЗУЕМ ТИП ЗДЕСЬ:
  const [form] = Form.useForm<AppSettings>();
  const [activeTab, setActiveTab] = useState('visual');
  const [rawJson, setRawJson] = useState('');
  const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false);

  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = async () => {
    try {
      setLoading(true);
      const response = await settingsService.get();
      const settings = response.data;

      // Заполняем форму визуального редактора
      form.setFieldsValue(settings);
      
      // Заполняем JSON редактор
      setRawJson(JSON.stringify(settings, null, 2));
    } catch (error) {
      message.error('Не удалось загрузить настройки');
    } finally {
      setLoading(false);
    }
  };

  const handleSaveVisual = async () => {
    try {
      setLoading(true);
      const values = await form.validateFields();
      
      // Структура формы совпадает с AppSettings, отправляем как есть
      await settingsService.update(values);
      
      // Обновляем JSON представление
      setRawJson(JSON.stringify(values, null, 2));
      message.success('Настройки сохранены!');
    } catch (error) {
      message.error('Проверьте правильность заполнения полей');
    } finally {
      setLoading(false);
    }
  };

  const handleSaveJson = async () => {
    try {
      setLoading(true);
      const parsed = JSON.parse(rawJson);

      // ЗАЩИТА: Строгая блокировка
      if (parsed.Auth || parsed.auth) {
        message.error('БЛОКИРОВКА: Изменение пароля через JSON запрещено! Используйте кнопку "Сменить пароль".');
        return; // <--- ПРЕРЫВАЕМ СОХРАНЕНИЕ
      }

      await settingsService.update(parsed);

      form.setFieldsValue(parsed);
      setRawJson(JSON.stringify(parsed, null, 2));
      message.success('JSON сохранен!');
    } catch (error) {
      message.error('Ошибка синтаксиса JSON');
    } finally {
      setLoading(false);
    }
  };

  const handleChangePassword = async (values: PasswordChangeValues) => {
    try {
      setLoading(true);
      await authService.changePassword(values.oldPassword, values.newPassword);
      message.success('Пароль изменен! Выполняется выход...');
      setIsPasswordModalOpen(false);
      // Разлогиниваем, чтобы он зашел с новым паролем
      await authService.logout();
      window.location.href = '/login';
    } catch (error: unknown) {
      const messageText = (error as any)?.response?.data?.message || 'Ошибка смены пароля';
      message.error(messageText);
    } finally {
      setLoading(false);
    }
  };

  const visualEditor = (
    <Form
      form={form}
      layout="vertical"
      onFinish={handleSaveVisual}
      initialValues={{ WorkerSettings: { MinRating: 4, CheckIntervalSeconds: 300 } }}
    >
      {/* СЕКЦИЯ: ИНТЕЛЛЕКТ */}
      <Divider>🧠 Интеллект и Правила</Divider>
      <Form.Item label="API Ключ OpenAI" name={['ApiKeys', 'OpenAI']} rules={[{ required: true }]}>
        <Input.Password placeholder="sk-..." />
      </Form.Item>

      <Form.Item label="Системный Промпт (Инструкция для AI)" name={['WorkerSettings', 'SystemPrompt']}>
        <TextArea rows={4} placeholder="Ты — вежливый менеджер..." />
      </Form.Item>

      <div style={{ display: 'flex', gap: '20px', flexWrap: 'wrap' }}>
        <Form.Item label="Минимальный рейтинг для ответа" name={['WorkerSettings', 'MinRating']}>
          <Rate count={5} />
        </Form.Item>
        
        <Form.Item label="Интервал проверки (сек)" name={['WorkerSettings', 'CheckIntervalSeconds']}>
          <InputNumber min={60} />
        </Form.Item>
      </div>

      <Form.Item 
        label="Подпись в конце ответа" 
        name={['WorkerSettings', 'Signature']}
        tooltip="Эта фраза будет автоматически добавляться к каждому ответу нейросети."
      >
        <Input placeholder="С уважением, Команда..." />
      </Form.Item>

      {/* СЕКЦИЯ: МАГАЗИНЫ */}
      <Divider>🛍️ Магазины</Divider>
      
      <Card type="inner" title="Wildberries" size="small" style={{marginBottom: 16}}>
        <Form.Item label="API Ключ (Токены 'Стандартный')" name={['ApiKeys', 'Wildberries']}>
            <Input.Password placeholder="eyJh..." />
        </Form.Item>
      </Card>

      <Card type="inner" title="Ozon (Мульти-аккаунт)" size="small">
        <Form.List name={['ApiKeys', 'OzonAccounts']}>
          {(fields, { add, remove }) => (
            <>
              {fields.map(({ key, name, ...restField }) => (
                <Row key={key} gutter={[8, 8]} align="bottom" style={{ marginBottom: 16, borderBottom: '1px solid #f0f0f0', paddingBottom: 16 }}>
                  <Col xs={24} md={8}>
                    <Form.Item
                      {...restField}
                      name={[name, 'ClientId']}
                      label="Client ID"
                      style={{ marginBottom: 0 }}
                      rules={[{ required: true, message: 'Required' }]}
                    >
                      <Input placeholder="123456" />
                    </Form.Item>
                  </Col>
                  <Col xs={20} md={14}>
                    <Form.Item
                      {...restField}
                      name={[name, 'ApiKey']}
                      label="API Key (Admin)"
                      style={{ marginBottom: 0 }}
                      rules={[{ required: true, message: 'Required' }]}
                    >
                      <Input.Password placeholder="xxxx-xxxx-..." />
                    </Form.Item>
                  </Col>
                  <Col xs={4} md={2} style={{ textAlign: 'right' }}>
                    <Button type="text" danger icon={<DeleteOutlined />} onClick={() => remove(name)} />
                  </Col>
                </Row>
              ))}
              <Form.Item style={{ marginTop: 16, marginBottom: 0 }}>
                <Button type="dashed" onClick={() => add()} block icon={<PlusOutlined />}>
                  Добавить магазин Ozon
                </Button>
              </Form.Item>
            </>
          )}
        </Form.List>
      </Card>

      {/* СЕКЦИЯ: БЕЗОПАСНОСТЬ */}
      <Divider>🔒 Безопасность</Divider>
      <Card type="inner" size="small" style={{ borderColor: '#ffa39e', backgroundColor: '#fff1f0' }}>
        <Form.Item label="Смена пароля администратора" style={{ marginBottom: 0 }}>
           <Button type="primary" danger onClick={() => setIsPasswordModalOpen(true)}>
             Сменить пароль
           </Button>
        </Form.Item>
      </Card>

      <div style={{ marginTop: 20, textAlign: 'right' }}>
         <Button type="primary" icon={<SaveOutlined />} onClick={handleSaveVisual} loading={loading} size="large">
            Сохранить настройки
         </Button>
      </div>
    </Form>
  );

  const jsonEditor = (
    <>
      <TextArea 
        rows={25} 
        value={rawJson} 
        onChange={(e) => setRawJson(e.target.value)} 
        style={{ fontFamily: 'monospace', fontSize: '14px', marginBottom: 16 }}
      />
      <Button type="primary" onClick={handleSaveJson} loading={loading}>
        Сохранить JSON
      </Button>
    </>
  );

  return (
    <Card
        title="Настройки системы"
        tabList={[{key: 'visual', tab: 'Визуальный редактор'}, {key: 'json', tab: 'JSON (Advanced)'}]}
        activeTabKey={activeTab}
        onTabChange={key => setActiveTab(key)}
    >
        {activeTab === 'visual' ? visualEditor : jsonEditor}

        {/* Модальное окно смены пароля */}
        <Modal
          title="Смена пароля"
          open={isPasswordModalOpen}
          onCancel={() => setIsPasswordModalOpen(false)}
          footer={null}
        >
          <Form
              layout="vertical"
              onFinish={handleChangePassword}
          >
              <Form.Item
                  label="Старый пароль"
                  name="oldPassword"
                  rules={[{ required: true, message: 'Введите старый пароль' }]}
              >
                  <Input.Password />
              </Form.Item>
              <Form.Item
                  label="Новый пароль"
                  name="newPassword"
                  rules={[{ required: true, message: 'Введите новый пароль' }, { min: 6, message: 'Минимум 6 символов' }]}
              >
                  <Input.Password />
              </Form.Item>
              <Form.Item>
                  <Button type="primary" htmlType="submit" block loading={loading}>
                      Обновить пароль
                  </Button>
              </Form.Item>
          </Form>
        </Modal>
    </Card>
  );
};

export default SettingsPage;